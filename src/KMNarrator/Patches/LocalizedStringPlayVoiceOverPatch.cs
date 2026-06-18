using Kingmaker.Localization;
using KMNarrator.Voice;

namespace KMNarrator.Patches
{
    internal static class LocalizedStringPlayVoiceOverPatch
    {
        public static void Postfix(LocalizedString __instance, VoiceOverStatus __result)
        {
            if (!Main.Enabled || __instance == null)
            {
                return;
            }

            Settings settings = Main.Settings;
            if (settings == null)
            {
                return;
            }

            if (__result != null)
            {
                return;
            }

            if (!VoiceOverHelper.IsUnvoiced(__instance) && settings.OnlyUnvoicedLines)
            {
                Main.LogVerbose("Skip PlayVoiceOver: Wwise event exists but playback failed (OnlyUnvoicedLines). key="
                    + __instance.Key);
                return;
            }

            if (!VoiceOverHelper.IsUnvoiced(__instance))
            {
                return;
            }

            string surface = VoiceSurface.Resolve();
            if (surface == "book" && settings.EnableBookEvents)
            {
                Main.LogVerbose("Skip PlayVoiceOver: book page handled by BookEventVoicePatch. key="
                    + __instance.Key);
                return;
            }

            Narrator.Speak(__instance, Narrator.TryGetDialogSpeakerHint(), surface);
        }
    }
}
