using System;
using System.Linq;
using System.Runtime.CompilerServices;
using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Utilities;
using LabApi.Features.Wrappers;
using MeowHint = HintServiceMeow.Core.Models.Hints.Hint;

namespace RespawnTimer
{
    // Хинт таймера через HintServiceMeow
    // У каждого игрока один постоянный хинт: раз в секунду меняется только его текст поэтому ничего не мигает
    // HintServiceMeow - мягкая зависимость: методы с его типами не инлайнятся поэтому без него плагин не падает
    public static class MeowHints
    {
        // Id хинта в HintServiceMeow чтобы найти его у игрока
        private const string HintId = "RespawnTimer";

        public static bool Available { get; private set; }

        public static void Initialize(Config config)
        {
            Available = config.UseHintServiceMeow && AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name.StartsWith("HintServiceMeow", StringComparison.OrdinalIgnoreCase));
        }

        // Показать или обновить хинт игроку
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Show(Player player, string text, Config config)
        {
            PlayerDisplay display = PlayerDisplay.Get(player.ReferenceHub);
            if (display.TryGetHint(HintId, out var existing) && existing is MeowHint hint)
            {
                hint.Text = text;
                hint.YCoordinate = config.HintY;
                hint.FontSize = config.HintFontSize;
                hint.Hide = false;
                return;
            }

            display.AddHint(new MeowHint
            {
                Id = HintId,
                Text = text,
                YCoordinate = config.HintY,
                FontSize = config.HintFontSize,
                Alignment = HintAlignment.Center,
            });
        }

        // Убрать хинт у игрока если он есть
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Remove(Player player)
        {
            PlayerDisplay display = PlayerDisplay.Get(player.ReferenceHub);
            if (display.HasHint(HintId))
                display.RemoveHint(HintId);
        }
    }
}
