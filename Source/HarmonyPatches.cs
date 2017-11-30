using Harmony;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace EasySpeedup
{
    [StaticConstructorOnStartup]
    public static class PatchConstructor
    {
        static PatchConstructor()
        {
            var harmony = HarmonyInstance.Create("EasySpeedup");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            // credits to @spdskatr#1657 for this one line beauty, changes 4x speed button to be an actual quad arrow
            ((Texture2D[])typeof(Thing).Assembly.GetType("Verse.TexButton").GetField("SpeedButtonTextures").GetValue(null))[4] =
                ContentFinder<Texture2D>.Get("UI/TimeControls/TimeSpeedButton_Ultrafast", true);
        }
    }


    [HarmonyPatch(typeof(TimeControls), "DoTimeControlsGUI")] // Target class for patching, generally required.
    public static class TimeControlsPatch
    {
        // stops checks for devmode enabled and draws/activates 4x speed mode
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // replace codeinstructions in order
            List<CodeInstruction> list = new List<CodeInstruction>(instructions);
            int stlocsLeft = 2;
            for (int i = 0; i < list.Count; i++)
            {
                // find opcode where they check if DevMode is being checked, and replace it with a True
                if (list[i].opcode == OpCodes.Call && list[i].operand == AccessTools.Property(typeof(Prefs), nameof(Prefs.DevMode)).GetGetMethod())
                {
                    CodeInstruction code = list[i];
                    code.opcode = OpCodes.Ldc_I4_1;
                    yield return code;
                    continue;
                }

                // yield latest codeinstruction
                yield return list[i];
                
                // find opcode before the check for the 5th speed option (ultra), replaces it with a true statement
                if (list[i].opcode == OpCodes.Stloc_S && --stlocsLeft == 0)
                {
                    i += 3;
                    CodeInstruction inst = list[i];
                    inst.opcode = OpCodes.Brtrue;
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(inst);
                }
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
