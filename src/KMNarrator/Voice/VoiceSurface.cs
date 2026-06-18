using Kingmaker;
using Kingmaker.Controllers.Dialog;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.GameModes;

namespace KMNarrator.Voice
{
    internal static class VoiceSurface
    {
        public static string Resolve()
        {
            try
            {
                Game game = Game.Instance;
                if (game != null && game.IsModeActive(GameModeType.Dialog))
                {
                    DialogController controller = game.DialogController;
                    BlueprintDialog dialog = controller != null ? controller.Dialog : null;
                    if (dialog != null && dialog.Type == DialogType.Book)
                    {
                        return "book";
                    }

                    return "dialog";
                }
            }
            catch
            {
            }

            return "bark";
        }
    }
}
