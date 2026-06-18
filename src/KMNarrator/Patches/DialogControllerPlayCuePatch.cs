using KMNarrator.Audio;

namespace KMNarrator.Patches
{
    internal static class DialogControllerPlayCuePatch
    {
        public static void PlayCuePrefix()
        {
            if (!Main.Enabled)
            {
                return;
            }

            AudioPlaybackService.Instance.Stop("DialogController.PlayCue");
        }
    }
}
