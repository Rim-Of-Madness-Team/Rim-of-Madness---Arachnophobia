﻿using RimWorld.Planet;
using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace Arachnophobia
{
    public class CompMultiHatcher : ThingComp
    {
        private float gestateProgress;

        public Pawn hatcheeParent;

        public Pawn otherParent;

        public Faction hatcheeFaction;

        public CompProperties_MultiHatcher Props
        {
            get
            {
                return (CompProperties_MultiHatcher)this.props;
            }
        }

        private CompTemperatureRuinable FreezerComp
        {
            get
            {
                return this.parent.GetComp<CompTemperatureRuinable>();
            }
        }

        protected bool TemperatureDamaged
        {
            get
            {
                CompTemperatureRuinable freezerComp = this.FreezerComp;
                return freezerComp != null && this.FreezerComp.Ruined;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref this.gestateProgress, "gestateProgress", 0f, false);
            Scribe_References.Look<Pawn>(ref this.hatcheeParent, "hatcheeParent", false);
            Scribe_References.Look<Pawn>(ref this.otherParent, "otherParent", false);
            Scribe_References.Look<Faction>(ref this.hatcheeFaction, "hatcheeFaction", false);
        }

        public override void CompTick()
        {
            //if (!this.TemperatureDamaged)
            //{
                float num = 1f / (this.Props.hatcherDaystoHatch * 60000f);
                this.gestateProgress += num;
                if (this.gestateProgress > 1f)
                {
                    this.Hatch();
                }
            //}
        }

        public void Hatch()
        {
            PawnGenerationRequest request = new PawnGenerationRequest(this.Props.hatcherPawn, this.hatcheeFaction, PawnGenerationContext.NonPlayer, -1, false, true, false, false, true, false, 1f, false, true, true, false, false, false, false, null, null, null, null, null, null);
            for (int i = 0; i < this.parent.stackCount; i++)
            {
                var range = this.Props.hatcherNumber.RandomInRange;
                for (int o = 0; o < range; o++)
                {
                    Pawn pawn = PawnGenerator.GeneratePawn(request);
                    if (PawnUtility.TrySpawnHatchedOrBornPawn(pawn, this.parent))
                    {
                        if (pawn != null)
                        {
                            if (this.hatcheeParent != null)
                            {
                                if (hatcheeParent.Faction != null) pawn.SetFaction(hatcheeParent.Faction);
                                if (pawn.playerSettings != null && this.hatcheeParent.playerSettings != null && this.hatcheeParent.Faction == this.hatcheeFaction)
                                {
                                    pawn.playerSettings.AreaRestriction = this.hatcheeParent.playerSettings.AreaRestriction;
                                }
                                if (pawn.RaceProps.IsFlesh)
                                {
                                    pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, this.hatcheeParent);
                                }
                            }
                            if (this.otherParent != null && (this.hatcheeParent == null || this.hatcheeParent.gender != this.otherParent.gender) && pawn.RaceProps.IsFlesh)
                            {
                                pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, this.otherParent);
                            }
                        }
                        if (this.parent.Spawned)
                        {
                            FilthMaker.MakeFilth(this.parent.Position, this.parent.Map, ThingDefOf.FilthAmnioticFluid, 1);
                        }
                    }
                    else
                    {
                        Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                    }

                }
            }
            this.parent.Destroy(DestroyMode.Vanish);
        }
        

        public override void PreAbsorbStack(Thing otherStack, int count)
        {
            float t = (float)count / (float)(this.parent.stackCount + count);
            var comp = ((ThingWithComps)otherStack).GetComp<CompMultiHatcher>();
            float b = comp.gestateProgress;
            this.gestateProgress = Mathf.Lerp(this.gestateProgress, b, t);
        }

        public override void PostSplitOff(Thing piece)
        {
            var comp = ((ThingWithComps)piece).GetComp<CompMultiHatcher>();
            comp.gestateProgress = this.gestateProgress;
            comp.hatcheeParent = this.hatcheeParent;
            comp.otherParent = this.otherParent;
            comp.hatcheeFaction = this.hatcheeFaction;
        }

        public override void PrePreTraded(TradeAction action, Pawn playerNegotiator, ITrader trader)
        {
            base.PrePreTraded(action, playerNegotiator, trader);
            if (action == TradeAction.PlayerBuys)
            {
                this.hatcheeFaction = Faction.OfPlayer;
            }
            else if (action == TradeAction.PlayerSells)
            {
                this.hatcheeFaction = trader.Faction;
            }
        }

        public override void PostPostGeneratedForTrader(TraderKindDef trader, int forTile, Faction forFaction)
        {
            base.PostPostGeneratedForTrader(trader, forTile, forFaction);
            this.hatcheeFaction = forFaction;
        }

        public override string CompInspectStringExtra()
        {
            if (!this.TemperatureDamaged)
            {
                return "EggProgress".Translate() + ": " + this.gestateProgress.ToStringPercent();
            }
            return null;
        }
    }
}
