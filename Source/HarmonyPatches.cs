using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using UnityEngine;

namespace EasySpeedup
{
    [StaticConstructorOnStartup]
    public static class PatchConstructor
    {
        static PatchConstructor()
        {
            var harmony = new Harmony("EasySpeedup");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            ((Texture2D[]) typeof(Thing).Assembly.GetType("Verse.TexButton").GetField(nameof(TexButton.SpeedButtonTextures)).GetValue(null))[4] =
                ContentFinder<Texture2D>.Get("UI/TimeControls/TimeSpeedButton_Ultrafast", true);
        }
    }

    [HarmonyPatch(typeof(TimeControls), nameof(TimeControls.DoTimeControlsGUI))] // Target class for patching, generally required.
    public static class TimeControlsPatch
    {
        private static readonly MethodInfo devGetter = AccessTools.Property(typeof(Prefs), nameof(Prefs.DevMode)).GetGetMethod();

        // stops checks for devmode enabled and draws/activates 4x speed mode
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // replace codeinstructions in order
            var list = new List<CodeInstruction>(instructions);
            var buttonDrawn = false;
            var devModeEnabled = false;

            for (var i = 0; i < list.Count; i++) {
                var code = list[i];

                // find opcode before the check for the 5th speed option (ultra), skips conditional
                if (!buttonDrawn && code.opcode == OpCodes.Ldloc_3) {
                    i += 3;
                    buttonDrawn = true;
                    yield return new CodeInstruction(list[i]);
                    continue;
                }

                // find opcode where they check if DevMode is enabled, and replace it with a True
                if (!devModeEnabled && code.Calls(devGetter)) {
                    code.opcode = OpCodes.Ldc_I4_1;
                    devModeEnabled = true;
                    yield return code;
                    continue;
                }

                // yield latest codeinstruction
                yield return code;
            }
        }

        // add room to the time rectangle for 5th speed button
        public static void Prefix(ref Rect timerRect)
        {
            timerRect.x -= 35f;
            timerRect.width += 35f;
        }
    }
}
