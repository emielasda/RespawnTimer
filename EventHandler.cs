using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Wrappers;

namespace RespawnTimer
{
    // Запускает/останавливает таймер по событиям раунда и отслеживает выбранную волну
    public sealed class EventHandler
    {
        private readonly RespawnTimer _timer;

        public EventHandler(RespawnTimer timer)
        {
            _timer = timer;
        }

        // Раунд начался - запускаем таймер
        public void OnRoundStarted() => _timer.Start();

        // Раунд закончился - останавливаем
        public void OnRoundEnded(RoundEndedEventArgs ev) => _timer.Stop();

        // Рестарт раунда - останавливаем
        public void OnRoundRestarted() => _timer.Stop();

        // Лобби (ожидание игроков) - останавливаем
        public void OnWaitingForPlayers() => _timer.Stop();

        // Игра выбрала волну - дальше будет анимация прибытия
        public void OnWaveTeamSelected(WaveTeamSelectedEventArgs ev) => _timer.SetSelectedWave(ev?.Wave?.Base);

        // Волна заспавнилась - выбранной волны больше нет
        public void OnWaveRespawned(WaveRespawnedEventArgs ev) => _timer.SetSelectedWave(null);

        // Для перезагрузки плагина посреди раунда
        public void StartIfRoundInProgress()
        {
            if (Round.IsRoundInProgress)
                _timer.Start();
        }
    }
}
