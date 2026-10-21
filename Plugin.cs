using System;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Loader.Features.Plugins;

namespace RespawnTimer
{
    // Точка входа LabAPI: таймер до респавна для наблюдателей
    public sealed class Plugin : Plugin<Config>
    {
        public static Plugin Instance { get; private set; }

        public override string Name => "RespawnTimer";

        public override string Description => "Показывает наблюдателям таймер до следующей волны респавна.";

        public override string Author => "emielasda";

        public override Version Version { get; } = new Version(1, 0, 0);

        // Версия LabAPI с которой собран плагин
        public override Version RequiredApiVersion { get; } = new Version(LabApiProperties.CompiledVersion);

        // Таймер с корутиной подсказок
        public RespawnTimer Timer { get; private set; }

        // Обработчики событий сервера
        public EventHandler Handler { get; private set; }

        public override void Enable()
        {
            Instance = this;
            if (Config == null || !Config.IsEnabled)
                return;

            MeowHints.Initialize(Config);
            Timer = new RespawnTimer(Config);
            Handler = new EventHandler(Timer);

            ServerEvents.RoundStarted += Handler.OnRoundStarted;
            ServerEvents.RoundEnded += Handler.OnRoundEnded;
            ServerEvents.RoundRestarted += Handler.OnRoundRestarted;
            ServerEvents.WaitingForPlayers += Handler.OnWaitingForPlayers;
            ServerEvents.WaveTeamSelected += Handler.OnWaveTeamSelected;
            ServerEvents.WaveRespawned += Handler.OnWaveRespawned;

            // Плагин включили посреди раунда (перезагрузка) - запускаем таймер сразу
            Handler.StartIfRoundInProgress();
        }

        public override void Disable()
        {
            if (Handler != null)
            {
                ServerEvents.RoundStarted -= Handler.OnRoundStarted;
                ServerEvents.RoundEnded -= Handler.OnRoundEnded;
                ServerEvents.RoundRestarted -= Handler.OnRoundRestarted;
                ServerEvents.WaitingForPlayers -= Handler.OnWaitingForPlayers;
                ServerEvents.WaveTeamSelected -= Handler.OnWaveTeamSelected;
                ServerEvents.WaveRespawned -= Handler.OnWaveRespawned;
            }

            Timer?.Stop();

            Handler = null;
            Timer = null;
            Instance = null;
        }
    }
}
