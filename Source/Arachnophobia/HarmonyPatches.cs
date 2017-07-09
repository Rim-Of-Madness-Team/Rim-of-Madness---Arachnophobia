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
            harmony.Patch(AccessTools.Method(typeof(Faction), "Notify_MemberDied"), new HarmonyMethod(typeof(HarmonyPatches).GetMethod("Notify_MemberDied_Prefix")), null);
            harmony.Patch(AccessTools.Method(typeof(CompEggLayer), "ProduceEgg"), null, new HarmonyMethod(typeof(HarmonyPatches).GetMethod("ProduceEgg_PostFix")));
        }

        // RimWorld.CompEggLayer
        public static void ProduceEgg_PostFix(CompEggLayer __instance, ref Thing __result)
        {
            var compHatcher = __result.TryGetComp<CompMultiHatcher>();
            if (compHatcher != null)
            {
                compHatcher.hatcheeFaction = __instance.parent.Faction;
                if (__instance.parent is Pawn pawn)
                {
                    compHatcher.hatcheeParent = pawn;
                }
                if (Traverse.Create(__instance).Field("fertilizedBy").GetValue<Pawn>() is Pawn Fertilizer)
                {
                    compHatcher.otherParent = Fertilizer;
                }
            }
        }


        // RimWorld.Faction
        public static bool Notify_MemberDied_Prefix(Faction __instance, Pawn member, DamageInfo? dinfo, bool wasWorldPawn) => (dinfo != null && dinfo.Value.Instigator is PawnWebSpinner p) ? false : true;

        // Verse.AI.Pawn_MindState
        public static bool StartManhunterBecauseOfPawnAction_PreFix(Pawn_MindState __instance)
        {
            if (__instance.pawn is PawnWebSpinner p && p?.CurJob?.def == ROMADefOf.ROMA_SpinPrey) return false;
            return true;
        }
    }
}
