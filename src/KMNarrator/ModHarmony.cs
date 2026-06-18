using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Controllers.Dialog;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Localization;
using Kingmaker.UI._ConsoleUI.Dialog.BookEvent;
using Kingmaker.UI.BookEvent;
using KMNarrator.Patches;
using UnityEngine;
using UnityModManagerNet;

namespace KMNarrator
{
    internal static class ModHarmony
    {
        private static Harmony _harmony;

        public static void Apply(UnityModManager.ModEntry modEntry)
        {
            if (_harmony != null)
            {
                return;
            }

            string id = modEntry.Info != null ? modEntry.Info.Id : "KMNarrator";
            _harmony = new Harmony(id);

            MethodInfo playVoiceOver = AccessTools.Method(
                typeof(LocalizedString),
                nameof(LocalizedString.PlayVoiceOver),
                new[] { typeof(MonoBehaviour) });
            PatchPostfix(
                playVoiceOver,
                typeof(LocalizedStringPlayVoiceOverPatch),
                nameof(LocalizedStringPlayVoiceOverPatch.Postfix),
                "LocalizedString.PlayVoiceOver(MonoBehaviour)");

            MethodInfo playCue = AccessTools.Method(
                typeof(DialogController),
                "PlayCue",
                new[] { typeof(BlueprintCueBase) });
            PatchPrefix(
                playCue,
                typeof(DialogControllerPlayCuePatch),
                nameof(DialogControllerPlayCuePatch.PlayCuePrefix),
                "DialogController.PlayCue");

            MethodInfo stopDialog = AccessTools.Method(typeof(DialogController), nameof(DialogController.StopDialog));
            PatchPostfix(
                stopDialog,
                typeof(DialogControllerStopDialogPatch),
                nameof(DialogControllerStopDialogPatch.StopDialogPostfix),
                "DialogController.StopDialog");

            PatchBookEventSetPage(typeof(BookEventVM), "BookEventVM.SetPage");
            PatchBookEventSetPage(typeof(BookEventBaseController), "BookEventBaseController.SetPage");

            Debug.Log("[KMNarrator] Harmony patches applied.");
        }

        private static void PatchBookEventSetPage(System.Type controllerType, string label)
        {
            MethodInfo setPage = AccessTools.Method(
                controllerType,
                "SetPage",
                new[]
                {
                    typeof(BlueprintBookPage),
                    typeof(List<CueShowData>),
                    typeof(List<BlueprintAnswer>)
                });
            PatchPrefix(
                setPage,
                typeof(BookEventVoicePatch),
                nameof(BookEventVoicePatch.SetPagePrefix),
                label + " (prefix)");
            PatchPostfix(
                setPage,
                typeof(BookEventVoicePatch),
                nameof(BookEventVoicePatch.SetPagePostfix),
                label + " (postfix)");
        }

        private static void PatchPrefix(MethodInfo target, System.Type patchType, string patchMethod, string label)
        {
            if (target == null)
            {
                Debug.LogWarning("[KMNarrator] " + label + " not found — hook skipped.");
                return;
            }

            _harmony.Patch(target, prefix: new HarmonyMethod(patchType, patchMethod));
        }

        private static void PatchPostfix(MethodInfo target, System.Type patchType, string patchMethod, string label)
        {
            if (target == null)
            {
                Debug.LogWarning("[KMNarrator] " + label + " not found — hook skipped.");
                return;
            }

            _harmony.Patch(target, postfix: new HarmonyMethod(patchType, patchMethod));
        }
    }
}
