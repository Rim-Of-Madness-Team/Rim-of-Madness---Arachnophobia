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
            harmony.Patch(AccessTools.Method(typeof(JobGiver_ReactToCloseMeleeThreat), "IsHunting"), null, new HarmonyMethod(typeof(HarmonyPatches).GetMethod("IsHunting_PostFix")), null);
            harmony.Patch(AccessTools.Method(typeof(PawnUtility), "IsFighting"), null, new HarmonyMethod(typeof(HarmonyPatches).GetMethod("IsFighting_PostFix")), null);
            harmony.Patch(AccessTools.Method(typeof(GenHostility), "GetPreyOfMyFaction"), null, new HarmonyMethod(typeof(HarmonyPatches).GetMethod("GetPreyOfMyFaction_PostFix")), null);
            harmony.Patch(AccessTools.Method(typeof(Faction), "Notify_MemberTookDamage"), null, new HarmonyMethod(typeof(HarmonyPatches).GetMethod("Notify_MemberTookDamage_PostFix")), null);
            harmony.Patch(AccessTools.Method(typeof(Pawn_MindState), "CanStartFleeingBecauseOfPawnAction"), null, new HarmonyMethod(typeof(HarmonyPatches).GetMethod("CanStartFleeingBecauseOfPawnAction")), null);
        }

        //MindState
        public static void CanStartFleeingBecauseOfPawnAction(Pawn p, ref bool __result)
        {
            if (p.ParentHolder is Building_Cocoon)
                __result = false;
        }

        // RimWorld.Faction
        public static void Notify_MemberTookDamage_PostFix(Faction __instance, Pawn member, DamageInfo dinfo)
        {
            if (dinfo.Instigator is Pawn p && p.CurJob != null && p.CurJob.def == ROMADefOf.ROMA_SpinPrey)
            {
                //Log.Message("Spiders GOOO");
                AccessTools.Method(typeof(Faction), "TookDamageFromPredator").Invoke(__instance, new object[] { p });
            }
        }


        // RimWorld.GenHostility
        private static void GetPreyOfMyFaction_PostFix(ref Pawn __result, Pawn predator, Faction myFaction)
        {
            if (predator?.CurJob is Job j && j.def == ROMADefOf.ROMA_SpinPrey && !predator.jobs.curDriver.ended)
            {
                Pawn pawn = j.GetTarget(TargetIndex.A).Thing as Pawn;
                if (pawn != null && pawn.Faction == myFaction)
                {
                    //Log.Message("Spiders GOOOO");
                    __result = pawn;
                }
            }
        }


        // RimWorld.PawnUtility
        public static void IsFighting_PostFix(ref bool __result, Pawn pawn)
        {
            __result = pawn.CurJob != null && (pawn.CurJob.def == JobDefOf.AttackMelee || pawn.CurJob.def == JobDefOf.AttackStatic || pawn.CurJob.def == JobDefOf.Wait_Combat || pawn.CurJob.def == JobDefOf.PredatorHunt || pawn.CurJob.def == ROMADefOf.ROMA_SpinPrey);
        }


        // RimWorld.JobGiver_ReactToCloseMeleeThreat
        public static void IsHunting_PostFix(ref bool __result, Pawn pawn, Pawn prey)
        {
            if (pawn?.CurJob == null)
            {
                __result = false;
                return;
            }
            if (!(pawn?.jobs?.curDriver is JobDriver_PredatorHunt))
            {
                JobDriver_SpinPrey jobDriver_Hunt = pawn.jobs.curDriver as JobDriver_SpinPrey;
                if (jobDriver_Hunt != null)
                {
                    //Log.Message("Spiders GOOOO");
                    __result = jobDriver_Hunt.Prey == prey;
                    return;
                }
            }
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
