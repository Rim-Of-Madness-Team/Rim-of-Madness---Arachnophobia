using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using System.Reflection;
using UnityEngine;

namespace Arachnophobia
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.jecrell.arachnophobia");
            harmony.Patch(AccessTools.Method(typeof(Pawn_MindState), "StartManhunterBecauseOfPawnAction"), new HarmonyMethod(typeof(HarmonyPatches).GetMethod("StartManhunterBecauseOfPawnAction_PreFix")), null);
        }

        // Verse.AI.Pawn_MindState
        public static bool StartManhunterBecauseOfPawnAction_PreFix(Pawn_MindState __instance)
        {
            if (__instance.pawn is PawnWebSpinner p && p?.CurJob?.def == ROMADefOf.ROMA_SpinPrey) return false;
            return true;
        }
    }
}
