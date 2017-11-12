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
        private bool resolvingCurrently = false;
        private PawnWebSpinner currentDrinker = null;

        public PawnWebSpinner CurrentDrinker
        {
            get
            {
                if (currentDrinker != null)
                {
                    if (!currentDrinker.Spawned || currentDrinker.Downed || currentDrinker.Dead ||
                        currentDrinker?.CurJob?.def != ROMADefOf.ROMA_ConsumeCocoon)
                        currentDrinker = null;
                }
                return currentDrinker;
            }
            set => currentDrinker = value;
        }
        public PawnWebSpinner Spinner { get { return spinner; } set { spinner = value; } }
        public Pawn Victim
        {
            get
            {
                Pawn result = null;
                if (this.innerContainer.Count > 0)
                {
                    if (this.innerContainer[0] is Pawn p) result = p;
                    if (this.innerContainer[0] is Corpse y) result = y.InnerPawn;
                }
                return result;
            }
        }

        public bool isPathableBy(Pawn p)
        {
            bool result = false;
            using (PawnPath pawnPath = p.Map.pathFinder.FindPath(p.Position, this.Position, TraverseParms.For(p, Danger.Deadly, TraverseMode.PassDoors, false), PathEndMode.OnCell))
            {
                if (!pawnPath.Found)
                {
                    return result;
                }
            }
            result = true;
            return result;
        }

        public bool isConsumableBy(Pawn pawn)
        {
            return pawn is PawnWebSpinner webSpinner &&
                    this.Spawned &&
                    !this.IsForbidden(pawn) &&
                    webSpinner.Spawned &&
                    !webSpinner.Dead &&
                    !webSpinner.IsBusy &&
                    webSpinner?.needs?.food?.CurLevelPercentage <= 0.4 &&
                    isConsumable &&
                    playerFactionExceptions(webSpinner) && 
                    isPathableBy(pawn) &&
                    pawn.CanReserve(this) &&
                    pawn.MapHeld.reservationManager.CanReserve(pawn, this) &&
                    !pawn.MapHeld.physicalInteractionReservationManager.IsReserved(this) &&
                    CurrentDrinker == null;
        }

        private IntVec3 nextValidPlacementSpot;
        public IntVec3 NextValidPlacementSpot
        {
            get
            {
                if (nextValidPlacementSpot == default(IntVec3) || nextValidPlacementSpot == IntVec3.Invalid)
                {
                    HashSet<Building_Cocoon> cocoonsToUpdate = new HashSet<Building_Cocoon>();
                    var cells = GenAdj.CellsAdjacent8Way(new TargetInfo(this.PositionHeld, this.MapHeld));
                    foreach (IntVec3 cell in cells)
                    {
                        if (cell.Walkable(this.MapHeld))
                        {
                            if (cell.GetThingList(this.Map).FirstOrDefault(x => x is Building_Cocoon) is Building_Cocoon c)
                            {
                                
                                cocoonsToUpdate.Add(c);
                            }
                            else if (cell.GetThingList(this.Map).FirstOrDefault(x => x is Building_Cocoon) == null)
                            {
                                if (GenConstruct.CanPlaceBlueprintAt(this.def, cell, Rot4.North, this.Map).Accepted)
                                {
                                    nextValidPlacementSpot = cell;
                                    break;
                                }
                            }
                        }
                         
                    }
                    foreach (Building_Cocoon c in cocoonsToUpdate)
                    {
                        if (!c.resolvingCurrently)
                            c.ResolvedNeighborPos();
                    }
                }
                return nextValidPlacementSpot;
            }
        }
        

        /// <summary>
        /// Finds a new position for a potential neighboring cocoon.
        /// </summary>
        public IntVec3 ResolvedNeighborPos()
        {
            resolvingCurrently = true;
            IntVec3 result = NextValidPlacementSpot;
            if (!result.Walkable(this.MapHeld))
            {
                nextValidPlacementSpot = default(IntVec3);
                result = NextValidPlacementSpot;
                for (int i = 0; i < 9; i++)
                {
                    if (result.GetThingList(this.Map).FirstOrDefault(x => x is Building_Cocoon) != null)
                    {
                        nextValidPlacementSpot = default(IntVec3);
                        result = NextValidPlacementSpot;
                        if (!GenConstruct.CanPlaceBlueprintAt(this.def, result, Rot4.North, this.Map).Accepted)
                        {
                            nextValidPlacementSpot = default(IntVec3);
                            result = NextValidPlacementSpot;
                        }
                        else break;
                    }
                    else break;
                }
            }
            resolvingCurrently = false;
            return result;
        }

        //Wild spiders || Faction spiders && player spiders must have access to cocoons
        public bool playerFactionExceptions(PawnWebSpinner y) => 
            (y?.Faction == null || 
            (Victim?.Faction != y?.Faction) && (y.Faction == Faction.OfPlayerSilentFail && this.PositionHeld.InAllowedArea(y)));

        /// <summary>
        /// Adds this cocoon to the lists in the MapComponent
        /// </summary>
        /// <param name="placer"></param>
        public void Notify_Placed(Pawn placer)
        {
            if (this.Map.GetComponent<MapComponent_CocoonTracker>() is MapComponent_CocoonTracker cocoons)
            {
                if (this.Faction == Faction.OfPlayer || placer?.Faction == Faction.OfPlayer)
                {
                    if (!cocoons.DomesticCocoons.Contains(this))
                        cocoons.DomesticCocoons.Add(this);
                }
                else
                {
                    if (!cocoons.WildCocoons.Contains(this))
                        cocoons.WildCocoons.Add(this);
                }
            }
            ResolvedNeighborPos();
        }

        /// <summary>
        /// Removes this cocoon to the lists in the MapComponent
        /// </summary>
        /// <param name="placer"></param>
        public void Notify_Removed(Pawn placer)
        {
            if (this.Map.GetComponent<MapComponent_CocoonTracker>() is MapComponent_CocoonTracker cocoons)
            {
                if (this.Faction == Faction.OfPlayer || placer?.Faction == Faction.OfPlayer)
                {
                    if (cocoons.DomesticCocoons.Contains(this))
                        cocoons.DomesticCocoons.Remove(this);
                }
                else
                {
                    if (cocoons.WildCocoons.Contains(this))
                        cocoons.WildCocoons.Remove(this);
                }
            }
            nextValidPlacementSpot = default(IntVec3);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Notify_Placed(null);
        }

        public override void DeSpawn()
        {
            Notify_Removed(null);
            base.DeSpawn();
        }

        // RimWorld.Building_Grave code repurposed for Cocoons
        private Graphic cachedGraphicFull;
        private Graphic cachedGraphicEmpty;
        public override Graphic Graphic
        {
            get
            {
                if (this.def.building.fullGraveGraphicData == null)
                {
                    return base.Graphic;
                }

                if (Victim == null)
                {
                    if (this.cachedGraphicEmpty == null)
                    {
                        this.cachedGraphicEmpty = GraphicDatabase.Get<Graphic_Single>(this.def.graphicData.texPath, ShaderDatabase.Cutout, this.def.graphicData.drawSize, this.DrawColor, this.DrawColorTwo, this.def.graphicData);
                    }
                    return this.cachedGraphicEmpty;
                }

                if (this.cachedGraphicFull == null)
                {
                    this.cachedGraphicFull = GraphicDatabase.Get<Graphic_Single>(this.def.building.fullGraveGraphicData.texPath, ShaderDatabase.Cutout, this.def.building.fullGraveGraphicData.drawSize, this.DrawColor, this.DrawColorTwo, this.def.building.fullGraveGraphicData);
                }
                return this.cachedGraphicFull;
            }
        }

        public bool isConsumable
        {
            get
            {
                return Victim != null &&
                        //Victim.IngestibleNow &&
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

        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % 250 == 0)
            {
                var nonSpinnerCarrier = (this?.ParentHolder is Pawn_CarryTracker c && c?.pawn is Pawn cp && !(cp is PawnWebSpinner)) && cp.Faction != Spinner.Faction ? cp : null;
                var isSpinnerAvailable = Spinner != null && Spinner.Spawned && !Spinner.IsBusy && Spinner.Map == nonSpinnerCarrier?.MapHeld;
                if (isSpinnerAvailable)
                {
                    var isInSpinnerLOS = GenSight.LineOfSight(Spinner.Position, nonSpinnerCarrier.Position, nonSpinnerCarrier.Map);
                    if (nonSpinnerCarrier != null && isInSpinnerLOS)
                    {
                        var attackJob = new Job(JobDefOf.AttackMelee, nonSpinnerCarrier);
                        attackJob.count = 1;
                        attackJob.killIncappedTarget = false;
                        Spinner.jobs.TryTakeOrderedJob(attackJob);
                    }
                }
                if (lastEscapeAttempt == 0) lastEscapeAttempt = Find.TickManager.TicksGame;

                if (Victim is Pawn p && !p.Dead &&
                    p.Faction == Faction.OfPlayerSilentFail &&
                    lastEscapeAttempt + GenDate.TicksPerHour > Find.TickManager.TicksGame)
                {
                    lastEscapeAttempt = Find.TickManager.TicksGame;
                    if (Rand.Value > 0.95f && !this.Destroyed)
                    {
                        Messages.Message("ROM_EscapedFromCocoon".Translate(p), MessageTypeDefOf.NeutralEvent);
                        this.EjectContents();
                    }
                }
            }
        }

        // RimWorld.Building_Casket
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (this.innerContainer.Count > 0 && (mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize))
            {
                if (mode != DestroyMode.Deconstruct)
                {
                    List<Pawn> list = new List<Pawn>();
                    foreach (Thing current in ((IEnumerable<Thing>)this.innerContainer))
                    {
                        Pawn pawn = current as Pawn;
                        if (pawn != null)
                        {
                            list.Add(pawn);
                        }
                    }
                    foreach (Pawn current2 in list)
                    {
                        HealthUtility.DamageUntilDowned(current2);
                    }
                }
                this.EjectContents();
            }
            this.innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
            if (!this.Destroyed) base.Destroy(mode);
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
            this.innerContainer.TryDropAll(this.PositionHeld, base.Map, ThingPlaceMode.Near);
            this.contentsKnown = true;
            if (!this.Destroyed) this.Destroy(DestroyMode.KillFinalize);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<PawnWebSpinner>(ref this.spinner, "spinner");
            Scribe_References.Look<PawnWebSpinner>(ref this.currentDrinker, "currentDrinker");
            Scribe_Values.Look<IntVec3>(ref this.nextValidPlacementSpot, "nextValidPlacementSpot", default(IntVec3));
        }
    }
}
