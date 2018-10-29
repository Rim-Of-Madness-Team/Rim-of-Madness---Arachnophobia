using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace Arachnophobia
{
    public class PawnWebSpinner : Pawn
    {
        private int webPeriod = -1;
        public int WebPeriod
        {
            get
            {
                if (webPeriod == -1)
                {
                    webPeriod = (int)Mathf.Lerp(3000f, 5000f, Rand.ValueSeeded(this.thingIDNumber ^ 74374237));
                }
                return webPeriod;
            }
        }

        private Thing web;
        public Thing Web { get { return web; } set { web = value; } }
        private int websMade = 0;
        public int WebsMade { get { return websMade; } set { websMade = value; } }
        public bool IsBusy
        {
            get
            {
                return this?.CurJob?.def == JobDefOf.PredatorHunt ||
                       this?.CurJob?.def == JobDefOf.Ingest ||
                       this?.CurJob?.def == JobDefOf.LayDown ||
                       this?.CurJob?.def == JobDefOf.Mate ||
                       this?.CurJob?.def == JobDefOf.LayEgg ||
                       this?.CurJob?.def == JobDefOf.AttackMelee ||
                       this?.CurJob?.def == ROMADefOf.ROMA_SpinPrey ||
                       this?.CurJob?.def == ROMADefOf.ROMA_ConsumeCocoon;
            }
        }
        public bool IsMakingCocoon => this?.CurJob?.def == ROMADefOf.ROMA_SpinPrey;
        public bool IsMakingWeb => this?.CurJob?.def == ROMADefOf.ROMA_SpinWeb;
        public bool CanMakeWeb {
            get {
                return !IsMakingWeb && !IsBusy && this.ageTracker.CurLifeStageIndex > 1;
            }
        }

        public void MakeWeb()
        {
            if (web == null && this.Spawned && !this.Downed && !this.Dead && CanMakeWeb)
            {
                var cell = RCellFinder.RandomWanderDestFor(this, this.PositionHeld, 5f, null, Danger.Some);
                this.jobs.StartJob(new Job(ROMADefOf.ROMA_SpinWeb, cell), JobCondition.InterruptForced);
            }
        }

        public void Notify_WebTouched(Pawn toucher)
        {
            if (web != null && this.Spawned && !this.Dead && !this.Downed)
            {
                //Our webspinners will attack prey under a few conditions.
                var hungryNow = this?.needs?.food?.CurCategory <= HungerCategory.Hungry;
                var canPreyUpon = (toucher?.RaceProps?.canBePredatorPrey ?? false) && (toucher?.RaceProps?.baseBodySize ?? 1) <= (this?.RaceProps?.maxPreyBodySize ?? 0);
                var attackAnyway = Rand.Value > 0.95; //5% chance to attack regardless
                var friendly = this?.Faction != null && (toucher?.Faction == this?.Faction || (!toucher.RaceProps.Animal && !toucher.Faction.HostileTo(this?.Faction) && !toucher.IsPrisonerOfColony));

                if (!friendly && (hungryNow && canPreyUpon || attackAnyway))
                {
                    Job spinPrey = new Job(ROMADefOf.ROMA_SpinPrey, toucher);
                    spinPrey.count = 1;
                    this.jobs.StartJob(spinPrey);
                }
            }
        }

        #region Overrides
        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % WebPeriod == 0)
            {
                //if (Utility.CocoonsFor(this.Map, this) is List<Thing> localCocoons &&
                //    !localCocoons.NullOrEmpty() && localCocoons.FirstOrDefault(x => x is Building_Cocoon y && y.Victim != null) is
                //    Building_Cocoon localCocoon &&
                //    (this?.needs?.food?.CurLevelPercentage ?? 0) < 0.35)
                //{
                //    var newJob = new Job(ROMADefOf.ROMA_ConsumeCocoon, localCocoon);
                //    newJob.locomotionUrgency = ((float)(localCocoon.Position - this.Position).LengthHorizontalSquared > 10f) ? LocomotionUrgency.Jog : LocomotionUrgency.Walk;
                //    this.jobs?.TryTakeOrderedJob(newJob);
                //}
                MakeWeb();
            }
            if (Find.TickManager.TicksGame % 1000 == 0)
            {
                CthulhuUtility.RemoveSanityLoss(this);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Thing>(ref this.web, "web");
            Scribe_Values.Look<int>(ref this.websMade, "websMade", 0);
        }
        #endregion Overrides
    }
}
