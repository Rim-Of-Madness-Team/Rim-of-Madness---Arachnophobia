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
                Building_Cocoon result = null;
                if (TargetA.Thing is Building_Cocoon cocoon) result = cocoon;
                return result;
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

            this.AddFinishAction(new Action(delegate
            {
                if (Cocoon != null)
                    Cocoon.CurrentDrinker = null;
            }));

            Toil gotoBody = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            gotoBody.AddPreInitAction(new Action(delegate
            {
                Cocoon.CurrentDrinker = this.pawn as PawnWebSpinner;

                if (Victim?.Faction == Faction.OfPlayerSilentFail &&
                    !Victim.Dead && 
                    !notifiedPlayer)
                {
                    notifiedPlayer = true;
                    var sound = (Victim?.Dead ?? false) ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.ThreatBig; 
                    Messages.Message("ROM_SpiderEatingColonist".Translate(new object[] { this.pawn.Label, Victim.Label }), sound);
                }

            }));

            //Toil executions
            yield return gotoBody;
            yield return Liquify();
            var durationMultiplier = 1f / this.pawn.GetStatValue(StatDefOf.EatingSpeed, true);
            yield return DrinkCorpse(durationMultiplier);
            yield return Toils_Ingest.FinalizeIngest(this.pawn, TargetIndex.B);
            yield return Toils_Jump.JumpIf(gotoBody, () => this.pawn.needs.food.CurLevelPercentage < 0.9f).FailOnDestroyedNullOrForbidden(TargetIndex.B);
        }

        public Toil Liquify()
        {
            //LIQUIFY - Burn all the victim's innards
            Toil liquify = new Toil();
            liquify.initAction = delegate
            {
                this.ticksLeft = Rand.Range(300, 900);
                this.pawn.rotationTracker.FaceCell(Cocoon.Position);
                this.job.SetTarget(TargetIndex.B, Victim.Corpse);
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
                        if (Victim.Dead) this.job.SetTarget(TargetIndex.B, Victim.Corpse);
                        this.ReadyForNextToil();
                    }
                    else ticksLeft = Rand.Range(300, 900);
                }
            };
            liquify.defaultCompleteMode = ToilCompleteMode.Never;
            liquify.WithEffect(EffecterDefOf.Vomit, TargetIndex.A);
            liquify.PlaySustainerOrSound(() => ROMADefOf.ROM_MeltingHiss);
            liquify.WithProgressBar(TargetIndex.A, delegate
            {
                var thing = this.pawn?.CurJob?.GetTarget(TargetIndex.B).Thing;
                if (thing == null)
                {
                    return 1f;
                }
                return 1f - (float)this.pawn.jobs.curDriver.ticksLeftThisToil / Mathf.Round((float)thing.def.ingestible.baseIngestTicks);
            }, false, -0.5f);
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
            this.report = "ROM_ConsumeJob2".Translate();
            Toil drinkCorpse = new Toil();
            drinkCorpse.initAction = delegate
            {
                Thing thing = Victim.Corpse;

                this.pawn.rotationTracker.FaceCell(TargetA.Thing.Position);

                if (!thing.IngestibleNow)
                {
                    this.pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
                    return;
                }

                this.pawn.jobs.curDriver.ticksLeftThisToil = Mathf.RoundToInt((float)thing.def.ingestible.baseIngestTicks * durationMultiplier);

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
            drinkCorpse.WithEffect(EffecterDefOf.EatMeat, TargetIndex.A);
            drinkCorpse.FailOnDestroyedOrNull(TargetIndex.B);
            drinkCorpse.PlaySustainerOrSound(() => SoundDefOf.RawMeat_Eat);
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
                Thing thing = this.job.GetTarget(TargetIndex.B).Thing;
                if (thing == null)
                {
                    return;
                }
                this.pawn.ClearAllReservations();
            });
            return drinkCorpse;
        }

        public override bool TryMakePreToilReservations()
        {
			return this.pawn.Reserve(this.Cocoon, this.job, 1, -1, null);
        }
    }
}
