using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        public bool CanConsume
        {
            get
            {
                return spinner != null &&
                        spinner.Spawned &&
                        !spinner.Dead &&
                        !spinner.IsBusy &&
                        spinner?.needs?.food?.CurLevelPercentage <= 0.4 &&
                        Victim != null;
            }
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
                    sound.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
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
            if (Victim is Pawn p && !p.Dead &&
                p.Faction == Faction.OfPlayerSilentFail &&
                lastEscapeAttempt + GenDate.TicksPerHour > Find.TickManager.TicksGame)
            {
                lastEscapeAttempt = Find.TickManager.TicksGame;
                if (Rand.Value > 0.95f && !this.Destroyed)
                {
                    Messages.Message("ROM_EscapedFromCocoon".Translate(p), MessageSound.Benefit);
                    this.EjectContents();
                    this.Destroy(DestroyMode.KillFinalize);
                }
            }

            if (CanConsume)
            {
                //Log.Message("JobStarted");
                var newJob = new Job(ROMADefOf.ROMA_ConsumeCocoon, this);
                newJob.locomotionUrgency = ((float)(this.Position - this.Position).LengthHorizontalSquared > 10f) ? LocomotionUrgency.Jog : LocomotionUrgency.Walk;
                Spinner.jobs?.TryTakeOrderedJob(newJob);
            }
        }


        [DebuggerHidden]
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo c in base.GetGizmos())
            {
                yield return c;
            }
            if (base.Faction == Faction.OfPlayer && this.innerContainer.Count > 0 && this.def.building.isPlayerEjectable)
            {
                Command_Action eject = new Command_Action();
                eject.action = new Action(this.EjectContents);
                eject.defaultLabel = "CommandPodEject".Translate();
                eject.defaultDesc = "CommandPodEjectDesc".Translate();
                if (this.innerContainer.Count == 0)
                {
                    eject.Disable("CommandPodEjectFailEmpty".Translate());
                }
                eject.hotKey = KeyBindingDefOf.Misc1;
                eject.icon = ContentFinder<Texture2D>.Get("UI/Commands/PodEject", true);
                yield return eject;
            }
        }
        
        public override void EjectContents()
        {
            ThingDef filthSlime = ThingDefOf.FilthSlime;
            foreach (Thing current in ((IEnumerable<Thing>)this.innerContainer))
            {
                if (current is Pawn pawn)
                {
                    PawnComponentsUtility.AddComponentsForSpawn(pawn);
                    pawn.filth.GainFilth(filthSlime);
                    //pawn.health.AddHediff(HediffDefOf.ToxicBuildup, null, null);
                    HealthUtility.AdjustSeverity(pawn, HediffDefOf.ToxicBuildup, 0.7f);
                }
            }
            if (!base.Destroyed)
            {
                sound.PlayOneShot(SoundInfo.InMap(new TargetInfo(base.Position, base.Map, false), MaintenanceType.None));
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
