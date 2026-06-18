using System.Text;
using System.Text.RegularExpressions;
using Kingmaker;
using Kingmaker.Localization;
using Kingmaker.TextTools;
using UnityEngine;

namespace KMNarrator.Voice
{
    internal static class TextNormalizer
    {
        private static readonly Regex RichTextTags = new Regex("<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex GlossaryTags = new Regex(@"\{(?:g\|[^}]*|/g)\}", RegexOptions.Compiled);
        private static readonly Regex Whitespace = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly Regex NarratorTemplateBlocks = new Regex(
            @"\{n\}.*?\{/n\}",
            RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex NarratorItalicBlocks = new Regex(
            @"<i>(?:[^<]|<(?!/i>))*</i>",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public static string Normalize(LocalizedString localized, bool includeStageDirections = true)
        {
            if (localized == null)
            {
                return string.Empty;
            }

            // Strip {n}…{/n} on raw pack text before TextTemplateEngine runs. Glossary {g|…}{/g}
            // inside narrator blocks becomes nested <color> tags when expanded; stripping colors
            // after expansion leaves partial stage directions (e.g. ", bows her head…").
            string raw = GetRawLocalizedText(localized);
            if (!string.IsNullOrEmpty(raw))
            {
                if (!includeStageDirections)
                {
                    raw = NarratorTemplateBlocks.Replace(raw, string.Empty);
                }

                string expanded = ProcessTemplates(raw);
                return NormalizeExpanded(expanded, includeStageDirections);
            }

            string fallback = localized;
            return NormalizeExpanded(fallback, includeStageDirections);
        }

        private static string NormalizeExpanded(string text, bool includeStageDirections)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            if (!includeStageDirections)
            {
                text = NarratorTemplateBlocks.Replace(text, string.Empty);
                text = StripNarratorColorBlocksBalanced(text);
                text = NarratorItalicBlocks.Replace(text, string.Empty);
            }

            text = RichTextTags.Replace(text, string.Empty);
            text = GlossaryTags.Replace(text, string.Empty);
            text = Whitespace.Replace(text, " ");
            return text.Trim();
        }

        private static string GetRawLocalizedText(LocalizedString localized)
        {
            LocalizationPack pack = LocalizationManager.CurrentPack;
            if (pack == null)
            {
                return string.Empty;
            }

            string key = ResolveLocalizationKey(localized);
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            return pack.GetText(key, true);
        }

        private static string ResolveLocalizationKey(LocalizedString localized)
        {
            if (localized.Shared == null)
            {
                return localized.Key;
            }

            LocalizedString current = localized.Shared.String;
            int depth = 0;
            while (current != null && current.Shared != null && depth < 50)
            {
                depth++;
                current = current.Shared.String;
            }

            return current != null ? current.Key : localized.Key;
        }

        private static string ProcessTemplates(string raw)
        {
            try
            {
                return TextTemplateEngine.Process(raw);
            }
            catch
            {
                return raw;
            }
        }

        // Fallback when stage directions lack {n} wrappers — match balanced narrator </color>.
        private static string StripNarratorColorBlocksBalanced(string text)
        {
            try
            {
                Game game = Game.Instance;
                if (game == null || game.BlueprintRoot == null)
                {
                    return text;
                }

                Color32 narrator = game.BlueprintRoot.UIRoot.DialogColors.Narrator;
                string hex = ColorUtility.ToHtmlStringRGB(narrator);
                string openNeedle = "<color=#" + hex;
                const string closeTag = "</color>";

                var output = new StringBuilder(text.Length);
                int index = 0;
                while (index < text.Length)
                {
                    int open = text.IndexOf(openNeedle, index, System.StringComparison.OrdinalIgnoreCase);
                    if (open < 0)
                    {
                        output.Append(text, index, text.Length - index);
                        break;
                    }

                    output.Append(text, index, open - index);
                    int tagEnd = text.IndexOf('>', open);
                    if (tagEnd < 0)
                    {
                        output.Append(text, open, text.Length - open);
                        break;
                    }

                    int depth = 1;
                    int pos = tagEnd + 1;
                    while (pos < text.Length && depth > 0)
                    {
                        int nestedOpen = text.IndexOf("<color=", pos, System.StringComparison.OrdinalIgnoreCase);
                        int close = text.IndexOf(closeTag, pos, System.StringComparison.OrdinalIgnoreCase);
                        if (close < 0)
                        {
                            pos = text.Length;
                            depth = 0;
                            break;
                        }

                        if (nestedOpen >= 0 && nestedOpen < close)
                        {
                            depth++;
                            int nestedEnd = text.IndexOf('>', nestedOpen);
                            pos = nestedEnd >= 0 ? nestedEnd + 1 : close + closeTag.Length;
                        }
                        else
                        {
                            depth--;
                            pos = close + closeTag.Length;
                        }
                    }

                    index = pos;
                }

                return output.ToString();
            }
            catch
            {
                return text;
            }
        }
    }
}
