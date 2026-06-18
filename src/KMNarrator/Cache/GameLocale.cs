using System;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;

namespace KMNarrator.Cache
{
    internal static class GameLocale
    {
        public static string GetCurrent()
        {
            try
            {
                Locale locale = LocalizationManager.CurrentLocale;
                string name = locale.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
            catch (Exception)
            {
            }

            return "enGB";
        }
    }
}
