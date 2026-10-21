using System;
using System.Collections.Generic;
using System.Text;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using Respawning;
using Respawning.Waves;
using Respawning.Waves.Generic;
using Logger = LabApi.Features.Console.Logger;

namespace RespawnTimer
{
    // Корутина которая раз в интервал считает время до волны и шлёт подсказку наблюдателям
    public sealed class RespawnTimer
    {
        // Таймер на паузе продолжает тикать пока до спавна больше 30 с (см WaveTimer.Update в игре)
        private const float PausedTickThreshold = 30f;

        // Сколько ошибок подряд логировать чтобы не засорять консоль каждую секунду
        private const int MaxLoggedErrors = 5;

        private readonly Config _config;
        private readonly List<Player> _spectators = new List<Player>();
        private readonly StringBuilder _builder = new StringBuilder(256);

        private CoroutineHandle _handle;          // текущая корутина (одна на сервер)
        private SpawnableWaveBase _selectedWave;  // волна которая уже прибывает
        private int _errorCount;                  // ошибок подряд для ограничения логов

        public RespawnTimer(Config config)
        {
            _config = config;
        }

        // Запускает таймер; старая корутина если была убивается
        public void Start()
        {
            Stop();
            _errorCount = 0;
            _handle = Timing.RunCoroutine(Run());
        }

        // Останавливает корутину и сбрасывает выбранную волну
        public void Stop()
        {
            if (_handle.IsRunning)
                Timing.KillCoroutines(new[] { _handle });

            _handle = default;
            _selectedWave = null;
            RemoveAllMeowHints();
        }

        // Волна которую игра уже выбрала для спавна (null - нет такой)
        public void SetSelectedWave(SpawnableWaveBase wave) => _selectedWave = wave;

        private IEnumerator<float> Run()
        {
            while (true)
            {
                float interval = Math.Max(0.2f, _config.UpdateInterval);

                // Любая ошибка ловится здесь чтобы корутина не умерла до конца раунда
                try
                {
                    Tick(interval);
                    _errorCount = 0;
                }
                catch (Exception e)
                {
                    if (_errorCount++ < MaxLoggedErrors)
                        Logger.Error($"[RespawnTimer] Ошибка в таймере: {e}");
                }

                yield return Timing.WaitForSeconds(interval);
            }
        }

        private void Tick(float interval)
        {
            _spectators.Clear();
            if (Round.IsRoundInProgress)
            {
                foreach (Player player in Player.List)
                {
                    if (IsSpectator(player))
                        _spectators.Add(player);
                }
            }

            string text = _spectators.Count > 0 ? BuildHint(_spectators.Count) : null;

            if (MeowHints.Available)
            {
                UpdateMeowHints(text);
                return;
            }

            if (text == null)
                return;

            // Чуть дольше интервала чтобы подсказка не мигала но и не висела долго поверх чужих
            float duration = interval + 0.1f;
            foreach (Player player in _spectators)
            {
                try
                {
                    player.SendHint(text, duration);
                }
                catch
                {
                    // Игрок мог выйти - просто пропускаем его
                }
            }
        }

        // Только живые люди в роли Spectator (Overwatch - отдельная роль и сюда не попадает)
        private static bool IsSpectator(Player player) =>
            player != null && !player.IsHost && !player.IsNpc && player.IsReady && player.Role == RoleTypeId.Spectator;

        // HintServiceMeow: наблюдателям обновляем текст хинта а у остальных его убираем
        private void UpdateMeowHints(string text)
        {
            foreach (Player player in Player.List)
            {
                if (player == null || player.IsHost || player.IsNpc || player.ReferenceHub == null)
                    continue;

                try
                {
                    if (text != null && IsSpectator(player))
                        MeowHints.Show(player, text, _config);
                    else
                        MeowHints.Remove(player);
                }
                catch
                {
                    // Игрок мог выйти - просто пропускаем его
                }
            }
        }

        // Убирает хинт HintServiceMeow у всех игроков
        private static void RemoveAllMeowHints()
        {
            if (!MeowHints.Available)
                return;

            foreach (Player player in Player.List)
            {
                if (player == null || player.IsHost || player.IsNpc || player.ReferenceHub == null)
                    continue;

                try
                {
                    MeowHints.Remove(player);
                }
                catch
                {
                    // Игрок мог выйти - просто пропускаем его
                }
            }
        }

        // Собирает текст подсказки
        // null - показывать нечего (респавн невозможен)
        private string BuildHint(int spectatorCount)
        {
            WaveQueueState state = WaveManager.State;
            bool spawning = state == WaveQueueState.WaveSelected || state == WaveQueueState.WaveSpawning;

            if (state == WaveQueueState.Idle)
                _selectedWave = null;

            SpawnableWaveBase nextWave;
            string time;

            if (spawning)
            {
                nextWave = _selectedWave;
                time = null;
            }
            else
            {
                if (!TryFindNextWave(out TimeBasedWave wave, out float eta))
                    return null;

                nextWave = wave;
                time = wave != null ? FormatTime(eta) : "--:--";
            }

            string team = FormatTeam(nextWave);
            return Render(time, team, spectatorCount, spawning);
        }

        // Ищет волну которая сработает раньше всех
        // Возвращает false если респавн в принципе невозможен
        // (все волны выключены в конфиге игры или без токенов)
        // next = null - все волны на бессрочной паузе
        private static bool TryFindNextWave(out TimeBasedWave next, out float eta)
        {
            next = null;
            eta = float.MaxValue;

            List<SpawnableWaveBase> waves = WaveManager.Waves;
            if (waves == null)
                return false;

            bool anyAvailable = false;
            foreach (SpawnableWaveBase baseWave in waves)
            {
                if (!(baseWave is TimeBasedWave wave) || wave.Timer == null)
                    continue;

                if (wave.Configuration != null && !wave.Configuration.IsEnabled)
                    continue;

                // Нет токенов: основная волна исчерпана или мини-волна ещё заблокирована
                if (wave is ILimitedWave limited && limited.RespawnTokens <= 0)
                    continue;

                anyAvailable = true;

                if (TryGetEta(wave.Timer, out float waveEta) && waveEta < eta)
                {
                    eta = waveEta;
                    next = wave;
                }
            }

            return anyAvailable;
        }

        // Реальное время до готовности волны с учётом паузы
        // false - пауза без срока
        private static bool TryGetEta(WaveTimer timer, out float eta)
        {
            eta = 0f;

            if (timer.IsForcefullyPaused)
                return false;

            float left = Math.Max(0f, timer.TimeLeft);
            if (!timer.IsPaused)
            {
                eta = left;
                return true;
            }

            // Пауза без таймера - например идёт Deadman Switch
            float pause = timer.PauseTimeLeft;
            if (pause <= 0f)
                return false;

            // На паузе таймер дотикивает до 30 с и ждёт её окончания
            eta = Math.Max(left - PausedTickThreshold, pause) + Math.Min(left, PausedTickThreshold);
            return true;
        }

        // Подставляет значения в шаблон HintText и добавляет вертикальный сдвиг
        private string Render(string time, string team, int spectatorCount, bool spawning)
        {
            _builder.Clear();

            // В HintServiceMeow позицию задаёт HintY поэтому сдвиг переносами только для обычного хинта
            int offset = MeowHints.Available ? 0 : Math.Max(-50, Math.Min(50, _config.HintVerticalOffset));
            for (int i = 0; i < offset; i++)
                _builder.Append('\n');

            string template = (_config.HintText ?? string.Empty).Replace("\r\n", "\n");
            bool first = true;
            foreach (string line in template.Split('\n'))
            {
                string result;
                if (line.Contains("{team}") && (!_config.ShowNextTeam || (team == null && _config.HideUnknownTeam)))
                    continue;

                if (line.Contains("{spectators}") && !_config.ShowSpectatorCount)
                    continue;

                if (spawning && line.Contains("{time}"))
                    result = _config.SpawningText ?? string.Empty;
                else
                    result = line
                        .Replace("{time}", time ?? string.Empty)
                        .Replace("{team}", team ?? _config.UnknownTeamText ?? string.Empty)
                        .Replace("{spectators}", spectatorCount.ToString());

                if (!first)
                    _builder.Append('\n');
                _builder.Append(result);
                first = false;
            }

            for (int i = 0; i < -offset; i++)
                _builder.Append('\n');

            return _builder.ToString();
        }

        // Название команды волны с цветом
        // null - команда неизвестна
        private string FormatTeam(SpawnableWaveBase wave)
        {
            if (wave == null)
                return null;

            string name;
            string color;
            switch (wave.TargetFaction)
            {
                case Faction.FoundationStaff:
                    name = _config.NtfName;
                    color = _config.NtfColor;
                    break;
                case Faction.FoundationEnemy:
                    name = _config.ChaosName;
                    color = _config.ChaosColor;
                    break;
                default:
                    return null;
            }

            if (wave is IMiniWave)
                name += _config.MiniWaveSuffix;

            color = NormalizeColor(color);
            return color == null ? name : $"<color={color}>{name}</color>";
        }

        // Приводит цвет к виду #RRGGBB; пустой - без цвета
        private static string NormalizeColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color))
                return null;

            color = color.Trim();
            return color[0] == '#' ? color : "#" + color;
        }

        // Секунды → мм:сс (округляем вверх чтобы 00:00 было только в момент спавна)
        private static string FormatTime(float seconds)
        {
            int total = (int)Math.Ceiling(Math.Max(0f, seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
