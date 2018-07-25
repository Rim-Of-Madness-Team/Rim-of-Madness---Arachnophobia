﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;
using RimWorld;
using RimWorld.Planet;
using System.Linq;

namespace Arachnophobia
{
    public class JobDriver_SpinPrey : JobDriver
    {
        private bool notifiedPlayer;

        private bool firstHit = true;

        public ThingDef CocoonDef
        {
            get
            {
                var result = ROMADefOf.ROMA_Cocoon;
                if ((Prey?.RaceProps?.baseBodySize ?? 0) > 0.99) result = ROMADefOf.ROMA_CocoonGiant;
                return result;
            }
        }
        public Pawn Prey
        {
            get
            {
                Corpse corpse = this.Corpse;
                if (corpse != null)
                {
                    return corpse.InnerPawn;
                }
                return (Pawn)base.job.GetTarget(TargetIndex.A).Thing;
            }
        }
        private Corpse Corpse
        {
            get
            {
                return base.job.GetTarget(TargetIndex.A).Thing as Corpse;
            }
        }
        string currentActivity = "";

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.firstHit, "firstHit", false, false);
        }

        public override string GetReport()
        {
            if (currentActivity == "") currentActivity = base.ReportStringProcessed(JobDefOf.Hunt.reportString);
            return currentActivity;
        }

        public IntVec3 CocoonPlace(Building_Cocoon exception = null)
        {
            var newPosition = TargetB.Cell;
            var localCocoons = Utility.CocoonsFor(this.pawn.Map, this.pawn);
            if (exception != null) localCocoons.Remove(exception);
            //Log.Message("1");
            if (localCocoons != null && localCocoons.Count > 0)
            {
                //Log.Message("2");

                var tempCocoons = new HashSet<Thing>(localCocoons.InRandomOrder());
                foreach (Building_Cocoon cocoon in tempCocoons)
                {
                    //Log.Message("3");
                    if (cocoon.NextValidPlacementSpot is IntVec3 vec && vec != default(IntVec3))
                    {
                        newPosition = vec;
                    }
                }
            }
            return newPosition;
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            /*
             * 
             *  Toil Configurations
             *
             */
            Toil prepareToSpin = new Toil();
            prepareToSpin.initAction = delegate
            {
                if (Prey == null && Corpse == null)
                {
                    this.pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
                    return;
                }
                if (Prey.Dead) this.pawn.CurJob.SetTarget(TargetIndex.A, Prey.Corpse);
            };

            Toil gotoBody = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            gotoBody.AddPreInitAction(new Action(delegate
                {
                    this.pawn.ClearAllReservations();
                    this.pawn.Reserve(TargetA, this.job);
                    //this.Map.physicalInteractionReservationManager.ReleaseAllForTarget(TargetA);
                    //this.Map.physicalInteractionReservationManager.Reserve(this.GetActor(), TargetA);
                    currentActivity = "ROM_SpinPreyJob1".Translate(TargetA.Thing?.LabelShort ?? "");
                }));

            Toil spinDelay = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = 500,
                initAction = delegate
                {
                    currentActivity = "ROM_SpinPreyJob2".Translate();
                }
            };
            spinDelay.WithProgressBarToilDelay(TargetIndex.B);

            Toil spinBody = new Toil
            {
                initAction = delegate
                {
                    //Log.Message("5");
                    var spinner = this.GetActor() as PawnWebSpinner;
                    if (spinner != null)
                    {
                        Building_Cocoon newCocoon;
                        Thing toLoad;
                        IntVec3 newPosition;
                        if (Prey.Dead)
                        {
                            toLoad = Prey.Corpse;
                            newPosition = Prey.Corpse.Position;
                        }
                        else
                        {
                            toLoad = Prey;
                            newPosition = Prey.Position;
                        }
                        if (!toLoad.Spawned) { this.EndJobWith(JobCondition.Incompletable); return; }

                        toLoad.DeSpawn();
                        toLoad.holdingOwner = null;
                        if (!GenConstruct.CanPlaceBlueprintAt(CocoonDef, newPosition, Rot4.North, this.pawn.Map).Accepted)
                        {
                            var cells = GenAdj.CellsAdjacent8Way(new TargetInfo(newPosition, this.pawn.Map));
                            foreach (IntVec3 cell in cells)
                            {
                                if (GenConstruct.CanPlaceBlueprintAt(CocoonDef, cell, Rot4.North, this.Map).Accepted)
                                {
                                    newPosition = cell;
                                    break;
                                }
                            }
                        }

                        newCocoon = (Building_Cocoon)GenSpawn.Spawn(CocoonDef, newPosition, spinner.Map);
                        newCocoon.Spinner = spinner;
                        //Log.Message("New Spinner: " + newCocoon.Spinner.Label);
                        newCocoon.TryGetInnerInteractableThingOwner().TryAdd(toLoad);
                        this.pawn?.CurJob?.SetTarget(TargetIndex.B, newCocoon);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };


            Toil pickupCocoon = Toils_Haul.StartCarryThing(TargetIndex.B);
            pickupCocoon.AddPreInitAction(new Action(delegate
            {
                //this.TargetB.Thing.DeSpawn();
                this.pawn.CurJob.SetTarget(TargetIndex.C, CocoonPlace((Building_Cocoon)TargetB.Thing));
                this.pawn.Reserve(TargetC, this.job);
                //this.pawn.Map.physicalInteractionReservationManager.Reserve(this.pawn, TargetC);
            }));
            Toil relocateCocoon = Toils_Haul.CarryHauledThingToCell(TargetIndex.C);
            Toil dropCocoon = Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, relocateCocoon, false).FailOn(() => !GenConstruct.CanPlaceBlueprintAt(CocoonDef, TargetC.Cell, Rot4.North, this.Map).Accepted);
            this.AddFinishAction(new Action(delegate
            {
                this.pawn.Map.physicalInteractionReservationManager.ReleaseAllClaimedBy(this.pawn);
            }));


        /*
         * 
         *  Toil Execution
         *
         */

        yield return new Toil
            {
                initAction = delegate
                {
                    this.Map.attackTargetsCache.UpdateTarget(this.pawn);
                },
                atomicWithPrevious = true,
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            Action onHitAction = delegate
            {
                Pawn prey = this.Prey;
                bool surpriseAttack = this.firstHit && !prey.IsColonist;
                if (this.pawn.meleeVerbs.TryMeleeAttack(prey, this.job.verbToUse, surpriseAttack))
                {
                    if (!this.notifiedPlayer && PawnUtility.ShouldSendNotificationAbout(prey))
                    {
                        this.notifiedPlayer = true;
                        Messages.Message("MessageAttackedByPredator".Translate(new object[]
                        {
                            prey.LabelShort,
                            this.pawn.LabelIndefinite()
                        }).CapitalizeFirst(), prey, MessageTypeDefOf.ThreatBig);// MessageSound.SeriousAlert);
                    }
                    this.Map.attackTargetsCache.UpdateTarget(this.pawn);
                }
                this.firstHit = false;
            };

            yield return Toils_Combat.FollowAndMeleeAttack(TargetIndex.A, onHitAction).JumpIf(() => Prey.Downed || Prey.Dead, prepareToSpin).FailOn(() => Find.TickManager.TicksGame > this.startTick + 5000 && (float)(this.job.GetTarget(TargetIndex.A).Cell - this.pawn.Position).LengthHorizontalSquared > 4f);
            yield return prepareToSpin.FailOn(() => Prey == null);
            yield return gotoBody.FailOn(() => Prey == null);
            yield return spinDelay.FailOn(() => Prey == null);
            yield return spinBody.FailOn(() => Prey == null);
            yield return pickupCocoon;
            yield return relocateCocoon;
            yield return dropCocoon;

            //float durationMultiplier = 1f / this.pawn.GetStatValue(StatDefOf.EatingSpeed, true);
            //yield return Toils_Ingest.ChewIngestible(this.pawn, durationMultiplier, TargetIndex.A, TargetIndex.None).FailOnDespawnedOrNull(TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            //yield return Toils_Ingest.FinalizeIngest(this.pawn, TargetIndex.A);
            //yield return Toils_Jump.JumpIf(gotoCorpse, () => this.pawn.needs.food.CurLevelPercentage < 0.9f);
        }

        public override bool TryMakePreToilReservations(bool showResult)
        {
            return true;
        }
    }
}
