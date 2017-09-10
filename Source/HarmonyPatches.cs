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
            HarmonyInstance.DEBUG = true;
            var harmony = HarmonyInstance.Create("EasySpeedup");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }


    [HarmonyPatch(typeof(TimeControls), "DoTimeControlsGUI")] // Target class for patching, generally required.
    public static class TimeControlsPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // replace the only blt at the end of the for (int index = 0; index < TimeControls.CachedTimeSpeedValues.Length; ++index) loop
            // with a bne, changing it to for (int index = 0; index == TimeControls.CachedTimeSpeedValues.Length; ++index)
            List<CodeInstruction> list = new List<CodeInstruction>(instructions);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].opcode == OpCodes.Call && list[i].operand == AccessTools.Property(typeof(Prefs), nameof(Prefs.DevMode)).GetGetMethod())
                {
                    CodeInstruction code = list[i];
                    code.opcode = OpCodes.Ldc_I4_1;
                    yield return code;
                    continue;
                }
                yield return list[i];
                if (list[i].opcode == OpCodes.Stloc_3)
                {
                    i += 3;
                    CodeInstruction inst = list[i];
                    inst.opcode = OpCodes.Brtrue;
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(inst);
                }
            }
        }

        public static void Prefix(ref Rect timerRect)
        {
            timerRect.x -= 30f;
            timerRect.width +=30f;
        }
    }
}
