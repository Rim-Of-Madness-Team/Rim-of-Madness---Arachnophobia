using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;
using RimWorld;
using System.Linq;
using UnityEngine;

namespace Arachnophobia
{
    public class JobDriver_ConsumeCocoon : JobDriver
    {
        public const TargetIndex PreyInd = TargetIndex.A;

        private const TargetIndex CorpseInd = TargetIndex.A;

        private bool notifiedPlayer = false;
        
        public Building_Cocoon Cocoon
        {
            get
            {
                return (Building_Cocoon)TargetA.Thing;
            }
        }

        public Pawn Victim
        {
            get
            {
                Pawn result = null;
                if (Cocoon != null && Cocoon.Victim is Pawn p) result = p;
                return result;
            }
        }

        public string report = "";
        public override string GetReport()
        {
            if (report == "")
            {
                report = base.ReportStringProcessed(JobDefOf.Ingest.reportString);
            }
            return report;
        }

        private int ticksLeft;

        
        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Toil definitions
            Toil gotoBody = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            gotoBody.AddPreInitAction(new Action(delegate
            {
                if (this.pawn.MapHeld.physicalInteractionReservationManager.IsReserved(Cocoon)) this.EndJobWith(JobCondition.Incompletable);
                this.pawn.MapHeld.physicalInteractionReservationManager.Reserve(this.pawn, Cocoon);

                if (Victim?.Faction == Faction.OfPlayerSilentFail &&
                    !Victim.Dead && 
                    !notifiedPlayer)
                {
                    notifiedPlayer = true;
                    var sound = (Victim?.Dead ?? false) ? MessageSound.Standard : MessageSound.SeriousAlert; 
                    Messages.Message("ROM_SpiderEatingColonist".Translate(new object[] { this.pawn.Label, Victim.Label }), sound);
                }

            }));

            //Toil executions
            yield return gotoBody.FailOnDespawnedOrNull(TargetIndex.A).FailOn(() => Victim == null || Cocoon == null || !Cocoon.Spawned || Cocoon.Destroyed);
            yield return Liquify().FailOnDespawnedOrNull(TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            var durationMultiplier = 1f / this.pawn.GetStatValue(StatDefOf.EatingSpeed, true);
            yield return DrinkCorpse(durationMultiplier).FailOnDespawnedOrNull(TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            //yield return Toils_Ingest.ChewIngestible(this.pawn, durationMultiplier, TargetIndex.B, TargetIndex.None).FailOnDespawnedOrNull(TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_Ingest.FinalizeIngest(this.pawn, TargetIndex.B);
            yield return Toils_Jump.JumpIf(gotoBody, () => this.pawn.needs.food.CurLevelPercentage < 0.9f).FailOnDestroyedNullOrForbidden(TargetIndex.B);
        }

        public Toil Liquify()
        {
            //Log.Message("2");
            if (Victim == null) this.EndJobWith(JobCondition.Incompletable);

            //LIQUIFY - Burn all the victim's innards
            Toil liquify = new Toil();
            liquify.initAction = delegate
            {
                this.ticksLeft = Rand.Range(300, 900);
                this.pawn.Drawer.rotator.FaceCell(Cocoon.Position);
                this.CurJob.SetTarget(TargetIndex.B, Victim.Corpse);
                this.notifiedPlayer = false;
                this.report = "ROM_ConsumeJob1".Translate();

            };
            liquify.tickAction = delegate
            {
                if (this.ticksLeft % 150 == 149)
                {
                    if (!Victim.Dead)
                    {
                        FilthMaker.MakeFilth(this.pawn.CurJob.targetA.Cell, this.Map, ThingDefOf.FilthSlime, this.pawn.LabelIndefinite(), 1);
                        var damageInt = (int)(2.5f * this.pawn.RaceProps.baseBodySize);
                        var damageToxic = (int)(25f * this.pawn.RaceProps.baseBodySize);
                        for (int i = 0; i < 2; i++)
                        {
                            var randomInternalOrgan = Victim?.health?.hediffSet?.GetNotMissingParts().InRandomOrder().FirstOrDefault(x => x.depth == BodyPartDepth.Inside);
                            if (!Victim.Destroyed || !Victim.Dead) Victim.TakeDamage(new DamageInfo(DamageDefOf.Burn, Rand.Range(damageInt, damageInt * 2), -1, this.pawn, randomInternalOrgan));
                            if (!Victim.Destroyed || !Victim.Dead) Victim.TakeDamage(new DamageInfo(ROMADefOf.ToxicBite, Rand.Range(damageToxic, damageToxic * 2), -1, this.pawn, randomInternalOrgan));
                        }
                    }
                    else
                    {
                        this.report = "ROM_ConsumeJob2".Translate();
                    }
                }
                this.ticksLeft--;
                if (this.ticksLeft <= 0)
                {
                    if (Victim.Dead || Victim.RaceProps.IsMechanoid)
                    {
                        if (Victim.Dead) this.CurJob.SetTarget(TargetIndex.B, Victim.Corpse);
                        this.ReadyForNextToil();
                    }
                    else ticksLeft = Rand.Range(300, 900);
                }
            };
            liquify.defaultCompleteMode = ToilCompleteMode.Never;
            liquify.WithEffect(EffecterDefOf.Vomit, TargetIndex.A);
            liquify.PlaySustainerOrSound(() => SoundDef.Named("HissSmall"));
            AddIngestionEffects(liquify, this.pawn, TargetIndex.B, TargetIndex.A);
            return liquify;
        }

        public void AddIngestionEffects(Toil toil, Pawn chewer, TargetIndex ingestibleInd, TargetIndex eatSurfaceInd)
        {
            //Log.Message("3");
            if (Victim == null) this.EndJobWith(JobCondition.Incompletable);

            toil.WithEffect(delegate
                {
                    LocalTargetInfo target = toil.actor.CurJob.GetTarget(ingestibleInd);
                    if (!target.HasThing)
                    {
                        return null;
                    }
                    EffecterDef result = target.Thing.def.ingestible.ingestEffect;
                    if (chewer.RaceProps.intelligence < Intelligence.ToolUser && target.Thing.def.ingestible.ingestEffectEat != null)
                    {
                        result = target.Thing?.def?.ingestible?.ingestEffectEat ?? null;
                    }
                    return result;
                }, delegate
                {
                    if (!toil.actor.CurJob.GetTarget(ingestibleInd).HasThing)
                    {
                        return null;
                    }
                    Thing thing = toil.actor.CurJob.GetTarget(ingestibleInd).Thing;
                    if (chewer != toil.actor)
                    {
                        return chewer;
                    }
                    if (eatSurfaceInd != TargetIndex.None && toil.actor.CurJob.GetTarget(eatSurfaceInd).IsValid)
                    {
                        return toil.actor?.CurJob?.GetTarget(eatSurfaceInd) ?? null;
                    }
                    return thing;
                });
                toil.PlaySustainerOrSound(delegate
                {
                    if (!chewer.RaceProps.Humanlike)
                    {
                        return null;
                    }
                    LocalTargetInfo target = toil.actor.CurJob.GetTarget(ingestibleInd);
                    if (!target.HasThing)
                    {
                        return null;
                    }
                    return target.Thing.def.ingestible.ingestSound;
                });
        }

        public Toil DrinkCorpse(float durationMultiplier)
        {
            if (Victim == null) this.EndJobWith(JobCondition.Incompletable); 
            //Log.Message("4");
            this.report = "ROM_ConsumeJob2".Translate();

            //Drink Corpse
            Toil drinkCorpse = new Toil();
            //Log.Message("5");
            drinkCorpse.initAction = delegate
            {
                //Log.Message("6");

                Thing thing = Victim.Corpse;
                //Log.Message("7");

                this.pawn.Drawer.rotator.FaceCell(TargetA.Thing.Position);
                //Log.Message("8");

                if (!thing.IngestibleNow)
                {
                    //Log.Message("8a");

                    this.pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
                    return;
                }
                //Log.Message("9");

                this.pawn.jobs.curDriver.ticksLeftThisToil = Mathf.RoundToInt((float)thing.def.ingestible.baseIngestTicks * durationMultiplier);
                //Log.Message("10");

            };
            drinkCorpse.tickAction = delegate
            {

                this.pawn.GainComfortFromCellIfPossible();
            };
            drinkCorpse.WithProgressBar(TargetIndex.A, delegate
            {
                var thing = this.pawn?.CurJob?.GetTarget(TargetIndex.B).Thing;
                if (thing == null)
                {
                    return 1f;
                }
                return 1f - (float)this.pawn.jobs.curDriver.ticksLeftThisToil / Mathf.Round((float)thing.def.ingestible.baseIngestTicks * durationMultiplier);
            }, false, -0.5f);
            drinkCorpse.defaultCompleteMode = ToilCompleteMode.Delay;
            drinkCorpse.FailOnDestroyedOrNull(TargetIndex.B);
            drinkCorpse.AddFinishAction(delegate
            {
                //Log.Message("11");

                if (this.pawn == null)
                {
                    return;
                }
                if (this.pawn.CurJob == null)
                {
                    return;
                }
                if (Cocoon == null)
                {
                    return;
                }
                Thing thing = this.pawn.CurJob.GetTarget(TargetIndex.B).Thing;
                if (thing == null)
                {
                    return;
                }
                if (this.pawn.Map.physicalInteractionReservationManager.IsReservedBy(this.pawn, TargetA.Thing))
                {
                    this.pawn.Map.physicalInteractionReservationManager.Release(this.pawn, TargetA);
                }
            });
            return drinkCorpse;
        }
    }
}
