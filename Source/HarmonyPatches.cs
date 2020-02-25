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
            ((Texture2D[]) typeof(Thing).Assembly.GetType("Verse.TexButton").GetField("SpeedButtonTextures").GetValue(null))[4] =
                ContentFinder<Texture2D>.Get("UI/TimeControls/TimeSpeedButton_Ultrafast", true);
        }
    }


    [HarmonyPatch(typeof(TimeControls), "DoTimeControlsGUI")] // Target class for patching, generally required.
    public static class TimeControlsPatch
    {
        private static MethodInfo devGetter = AccessTools.Property(typeof(Prefs), nameof(Prefs.DevMode)).GetGetMethod();
        // stops checks for devmode enabled and draws/activates 4x speed mode
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // replace codeinstructions in order
            List<CodeInstruction> list = new List<CodeInstruction>(instructions);
            bool buttonDrawn = false,
                 devModeEnabled = false;

            for (int i = 0; i < list.Count; i++) {
                // find opcode before the check for the 5th speed option (ultra), skips conditional
                if (!buttonDrawn && list[i].opcode == OpCodes.Ldloc_3) {
                    i += 3;
                    buttonDrawn = true;
                    yield return new CodeInstruction(list[i]);
                    continue;
                }

                // find opcode where they check if DevMode is enabled, and replace it with a True
                if (!devModeEnabled && list[i].opcode == OpCodes.Call && list[i].operand == devGetter) {
                    CodeInstruction code = list[i];
                    code.opcode = OpCodes.Ldc_I4_1;
                    devModeEnabled = true;
                    yield return code;
                    continue;
                }

                // yield latest codeinstruction
                yield return list[i];
            }
        }

        // add room to the time rectangle for 5th speed button
        public static void Prefix(ref Rect timerRect)
        {
            timerRect.x -= 35f;
            timerRect.width +=35f;
        }
    }
}
