using Kingmaker.Localization;

namespace KMNarrator
{
    // Mirrors Kingmaker.Localization.LocalizedString.PlayVoiceOver SoundPack lookup.
    internal static class VoiceOverHelper
    {
        public static string GetVoiceOverSound(LocalizedString text)
        {
            if (text == null || LocalizationManager.SoundPack == null)
            {
                return "";
            }

            string key = ResolveSoundKey(text);
            if (string.IsNullOrEmpty(key))
            {
                return "";
            }

            return LocalizationManager.SoundPack.GetText(key, false);
        }

        public static bool IsUnvoiced(LocalizedString text)
        {
            return string.IsNullOrEmpty(GetVoiceOverSound(text));
        }

        private static string ResolveSoundKey(LocalizedString text)
        {
            if (text.Shared == null)
            {
                return text.Key;
            }

            LocalizedString current = text.Shared.String;
            int depth = 0;
            while (current != null && current.Shared != null && depth < 50)
            {
                depth++;
                current = current.Shared.String;
            }

            return current != null ? current.Key : text.Key;
        }
    }
}
