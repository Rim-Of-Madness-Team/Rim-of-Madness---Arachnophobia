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
        private bool firstTick;

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
                    this.jobs.StartJob(spinPrey, JobCondition.InterruptForced);
                }
            }
        }

        #region Overrides

        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % 60 == 0 && !firstTick)
            {
                firstTick = true;
                //Are we a player animal? Great.
                if (this.Faction == Faction.OfPlayerSilentFail)
                    return;

                //Are there more of us? Great.
                if (this.MapHeld.mapPawns.AllPawnsSpawned.FirstOrDefault(x => x != this && x.def == this.def) != null)
                    return;

                //Only one? Let's fix that.
                if (this.def == ROMADefOf.ROMA_SpiderRace)
                {
                    if (this.MapHeld.GetComponent<MapComponent_CocoonTracker>() is MapComponent_CocoonTracker tracker && !tracker.isSpiderPair)
                    {
                        tracker.isSpiderPair = true;
                        Pawn newThing = PawnGenerator.GeneratePawn(this.kindDef, this.Faction);
                        var newSpinner = (PawnWebSpinner)GenSpawn.Spawn((PawnWebSpinner)newThing, this.PositionHeld, this.MapHeld);
                        newSpinner.gender = (this.gender == Gender.Male) ? Gender.Female : Gender.Male;
                    }
                }
                if (this.def == ROMADefOf.ROMA_SpiderRaceGiant)
                {
                    if (this.MapHeld.GetComponent<MapComponent_CocoonTracker>() is MapComponent_CocoonTracker tracker && !tracker.isGiantSpiderPair)
                    {
                        tracker.isGiantSpiderPair = true;
                        Pawn newThing = PawnGenerator.GeneratePawn(this.kindDef, this.Faction);
                        var newSpinner = (PawnWebSpinner)GenSpawn.Spawn((PawnWebSpinner)newThing, this.PositionHeld, this.MapHeld);
                        newSpinner.gender = (this.gender == Gender.Male) ? Gender.Female : Gender.Male;
                    }
                }
            }

            if (Find.TickManager.TicksGame % WebPeriod == 0)
            {
                //Have a spinneret? Make some web.
                if (this?.health?.hediffSet?.GetNotMissingParts()?.FirstOrDefault(x => x.def.defName == "ROMA_Spinneret") != null)
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
            Scribe_Values.Look(ref firstTick, "firstTick", false);
            Scribe_Values.Look<int>(ref this.websMade, "websMade", 0);
        }
        #endregion Overrides
    }
}
