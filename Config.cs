using System.ComponentModel;

namespace RespawnTimer
{
    // Конфиг плагина (configs/<порт>/RespawnTimer/config.yml)
    public sealed class Config
    {
        [Description("Включён ли плагин")]
        public bool IsEnabled { get; set; } = true;

        [Description("Интервал обновления подсказки в секундах (минимум 0.2) - подсказка живёт чуть дольше интервала чтобы не мигала")]
        public float UpdateInterval { get; set; } = 1f;

        [Description("Шаблон подсказки: {time} - таймер мм:сс; {team} - следующая волна; {spectators} - число наблюдателей; " +
                     "строка с {team} убирается при ShowNextTeam: false; строка с {spectators} убирается при ShowSpectatorCount: false; " +
                     "строка с {time} заменяется на SpawningText пока волна прибывает; теги вроде <size> закрывайте в той же строке")]
        public string HintText { get; set; } =
            "До респавна: <b>{time}</b>\nСледующая волна: {team}\nНаблюдателей: {spectators}";

        [Description("Текст вместо строки с таймером пока идёт анимация вертолёта/машины")]
        public string SpawningText { get; set; } = "Подкрепление прибывает!";

        [Description("Текст если игра ещё не определила какая волна будет следующей")]
        public string UnknownTeamText { get; set; } = "Неизвестно";

        [Description("Скрывать строку с командой когда она неизвестна (вместо UnknownTeamText)")]
        public bool HideUnknownTeam { get; set; } = false;

        [Description("Показывать строку со следующей командой")]
        public bool ShowNextTeam { get; set; } = true;

        [Description("Показывать строку с количеством наблюдателей")]
        public bool ShowSpectatorCount { get; set; } = true;

        [Description("Название МОГ в подсказке")]
        public string NtfName { get; set; } = "МОГ";

        [Description("Название Хаоса в подсказке")]
        public string ChaosName { get; set; } = "Хаос";

        [Description("Приписка к названию команды если следующая волна - мини-волна (пусто - не писать)")]
        public string MiniWaveSuffix { get; set; } = " (мини-волна)";

        [Description("HEX-цвет МОГ")]
        public string NtfColor { get; set; } = "#0096FF";

        [Description("HEX-цвет Хаоса")]
        public string ChaosColor { get; set; } = "#1E9E2E";

        [Description("Сдвиг подсказки переносами строк: > 0 - ниже; < 0 - выше; 0 - без сдвига (только без HintServiceMeow)")]
        public int HintVerticalOffset { get; set; } = 0;

        [Description("Показывать подсказку через HintServiceMeow если он стоит на сервере (иначе обычный хинт игры)")]
        public bool UseHintServiceMeow { get; set; } = true;

        [Description("HintServiceMeow: высота подсказки на экране (0 - верх; 1080 - низ)")]
        public float HintY { get; set; } = 780f;

        [Description("HintServiceMeow: размер шрифта")]
        public int HintFontSize { get; set; } = 28;
    }
}
