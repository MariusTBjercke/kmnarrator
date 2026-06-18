using KMNarrator.Audio;

namespace KMNarrator.Patches
{
    internal static class DialogControllerStopDialogPatch
    {
        public static void StopDialogPostfix()
        {
            if (!Main.Enabled)
            {
                return;
            }

            AudioPlaybackService.Instance.Stop("DialogController.StopDialog");
        }
    }
}
