using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Arachnophobia
{
    public class Building_Cocoon : Building_Casket
    {
        private PawnWebSpinner spinner;
        public PawnWebSpinner Spinner { get { return spinner; } set { spinner = value; } }
        public bool isConsumableBy(Pawn pawn)
        {
                return  (spinner?.kindDef == pawn?.kindDef || pawn?.kindDef == ROMADefOf.ROMA_SpiderKindGiantQueen) &&
                        pawn is PawnWebSpinner webSpinner &&
                        webSpinner.Spawned &&
                        !webSpinner.Dead &&
                        !webSpinner.IsBusy &&
                        webSpinner?.needs?.food?.CurLevelPercentage <= 0.4 &&
                        isConsumable;
        }

        public bool isConsumable
        {
            get
            {
                return Victim != null &&
                        this.Spawned &&
                        !this.Destroyed &&
                        !this.MapHeld.physicalInteractionReservationManager.IsReserved(this);
            }
        }

        public override string GetInspectString()
        {
            var str = this.innerContainer.ContentsString;
            var str2 = "None".Translate();
            var compDisappearsStr = this.GetComp<CompLifespan>()?.CompInspectStringExtra()?.TrimEndNewlines() ?? "";
            var result = new StringBuilder();

            if (Spinner != null)
            {
                str2 = (spinner.Faction != Faction.OfPlayerSilentFail) ? "ROM_Wild".Translate(spinner.Label) : spinner.Name.ToStringFull;
                if (spinner.Dead) str2 = "DeadLabel".Translate(str2);
            }
            result.AppendLine("ROM_Spinner".Translate() + ": " + str2);

            result.AppendLine("CasketContains".Translate() + ": " + str.CapitalizeFirst());
            result.AppendLine(compDisappearsStr);

            return result.ToString().TrimEndNewlines();
        }

        public Pawn Victim
        {
            get
            {
                Pawn result = null;
                if (this.innerContainer.Count > 0)
                {
                    if(this.innerContainer[0] is Pawn p) result = p;
                    if (this.innerContainer[0] is Corpse y) result = y.InnerPawn;
                }
                return result;
            }
        }

        private SoundDef sound = SoundDef.Named("HissSmall");

        public override bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
        {
            if (base.TryAcceptThing(thing, allowSpecialEffects))
            {
                if (allowSpecialEffects)
                {
                    //sound.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
                }
                return true;
            }
            return false;
        }

        public int lastEscapeAttempt = 0;

        public override void TickRare()
        {
            base.TickRare();
            if (lastEscapeAttempt == 0) lastEscapeAttempt = Find.TickManager.TicksGame;
            if (this.holdingOwner?.Owner is Pawn holder)
            {
                Log.Message("1");
            }
            if (Victim is Pawn p && !p.Dead &&
                p.Faction == Faction.OfPlayerSilentFail &&
                lastEscapeAttempt + GenDate.TicksPerHour > Find.TickManager.TicksGame)
            {
                lastEscapeAttempt = Find.TickManager.TicksGame;
                if (Rand.Value > 0.95f && !this.Destroyed)
                {
                    Messages.Message("ROM_EscapedFromCocoon".Translate(p), MessageSound.Benefit);
                    this.EjectContents();
                }
            }


        }
        

        [DebuggerHidden]
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo c in base.GetGizmos())
            {
                yield return c;
            }
        }
        
        public override void EjectContents()
        {
            ThingDef filthCobwebs = ROMADefOf.ROM_FilthCobwebs;
            foreach (Thing current in ((IEnumerable<Thing>)this.innerContainer))
            {
                if (current is Pawn pawn)
                {
                    PawnComponentsUtility.AddComponentsForSpawn(pawn);
                    pawn.filth.GainFilth(filthCobwebs);
                    //pawn.health.AddHediff(HediffDefOf.ToxicBuildup, null, null);
                    HealthUtility.AdjustSeverity(pawn, HediffDefOf.ToxicBuildup, 0.3f);
                }
            }
            if (!base.Destroyed)
            {
                //sound.PlayOneShot(SoundInfo.InMap(new TargetInfo(base.Position, base.Map, false), MaintenanceType.None));
            }
            base.EjectContents();
            if (!this.Destroyed) this.Destroy(DestroyMode.KillFinalize);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<PawnWebSpinner>(ref this.spinner, "spinner");
        }
    }
}
