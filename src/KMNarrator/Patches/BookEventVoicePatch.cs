using System.Collections.Generic;
using Kingmaker.Controllers.Dialog;
using Kingmaker.DialogSystem.Blueprints;
using KMNarrator.Audio;
using KMNarrator.Voice;

namespace KMNarrator.Patches
{
    // Book events queue cues then drain them in OnUpdate via PlayVoiceOver. Unvoiced lines return
    // null immediately, so the queue empties in one frame and ordered TTS never lines up. Narrate
    // the full page when SetPage runs instead (PC + console UI).
    internal static class BookEventVoicePatch
    {
        public static void SetPagePrefix()
        {
            if (!Main.Enabled)
            {
                return;
            }

            AudioPlaybackService.Instance.Stop("BookEvent.SetPage");
        }

        public static void SetPagePostfix(List<CueShowData> cues)
        {
            if (!Main.Enabled)
            {
                return;
            }

            Settings settings = Main.Settings;
            if (settings == null || !settings.EnableBookEvents || cues == null)
            {
                return;
            }

            string speakerHint = Narrator.TryGetDialogSpeakerHint();
            foreach (CueShowData data in cues)
            {
                BlueprintCue cue = data != null ? data.Cue : null;
                if (cue == null)
                {
                    continue;
                }

                bool isNarratorCue = cue.Speaker != null && cue.Speaker.NoSpeaker;
                Narrator.Speak(cue.Text, speakerHint, "book", isNarratorCue);
            }
        }
    }
}
