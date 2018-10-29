using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace Arachnophobia
{
    public class Building_Web : Building
    {
        private PawnWebSpinner spinner;
        public PawnWebSpinner Spinner { get { return spinner; } set { spinner = value; } }

        public void WebEffect(Pawn p)
        {
            try
            {
                if (p != null)
                {
                    if (WebShouldAffect(p))
                    {
                        float num = 20f;
                        float num2 = Mathf.Lerp(0.85f, 1.15f, p.thingIDNumber ^ 74374237);
                        num *= num2;
                        p.TakeDamage(new DamageInfo(DamageDefOf.Stun, (int)num, 1f, -1, this));
                        if (!Destroyed)
                        {
                            var leavingsRect = new CellRect(this.OccupiedRect().minX, this.OccupiedRect().minZ, this.OccupiedRect().Width, this.OccupiedRect().Height);
                            if (Rand.Value > 0.9)
                            {
                                this.Destroy(DestroyMode.KillFinalize);
                            }
                            else
                            {
                                for (int i = leavingsRect.minZ; i <= leavingsRect.maxZ; i++)
                                {
                                    for (int j = leavingsRect.minX; j <= leavingsRect.maxX; j++)
                                    {
                                        IntVec3 c = new IntVec3(j, 0, i);
                                        if (Rand.Value > 0.5f) FilthMaker.MakeFilth(c, this.Map, this.def.filthLeaving, Rand.RangeInclusive(1, 3));
                                    }
                                }
                                this.Destroy(DestroyMode.Vanish);
                            }
                        }

                        if (spinner != null) spinner.Notify_WebTouched(p);
                        if (p?.Faction == Faction.OfPlayerSilentFail) Messages.Message("ROM_SpiderWebsCrossed".Translate(p.LabelShort), p, MessageTypeDefOf.NeutralEvent);
                        spinner.Web = null;
                    }
                }
            }
            catch (Exception e) { /* Log.Message(e.ToString()); */ }


        }

        private bool WebShouldAffect(Pawn p)
        {
            bool isViableAnimal = p?.RaceProps?.Animal == true && p?.Faction != Faction.OfPlayer && p?.HostFaction?.IsPlayer == false;
            bool isViablePrey = (p?.Faction?.HostileTo(Spinner.Faction) == true || isViableAnimal);
            bool isPlayerSpinner = Spinner?.Faction == Faction.OfPlayer && isViablePrey;
            return isPlayerSpinner || Spinner?.Faction != Faction.OfPlayer;
        }

        private HashSet<Pawn> touchingPawns = new HashSet<Pawn>();
        public override void Tick()
        {
            HashSet<Thing> thingList = new HashSet<Thing>(base.Position.GetThingList(base.Map));
            foreach (Thing t in thingList)
            {
                Pawn pawn = t as Pawn;
                if (pawn != null && (!(pawn is PawnWebSpinner)) && !this.touchingPawns.Contains(pawn))
                {
                    this.touchingPawns.Add(pawn);
                    this.WebEffect(pawn);
                }
            }
            HashSet<Pawn> temp = new HashSet<Pawn>(touchingPawns);
            foreach (Pawn p in temp)
            {
                if (!p.Spawned || p.Position != base.Position)
                {
                    this.touchingPawns.Remove(p);
                }
            }
            base.Tick();
            //try
            //{
            //    if (!this.Destroyed && this.Spawned && Find.TickManager.TicksGame % 20 == 0)
            //    {
            //        var mapHeld = this.MapHeld;
            //        if (mapHeld != null)
            //        {
            //            var cells = new List<IntVec3>(this?.OccupiedRect().Cells?.ToList() ?? null);
            //            var cellCount = (!cells.NullOrEmpty()) ? cells.Count : 0;
            //            for (int j = 0; j < cellCount; j++)
            //            {
            //                var thingList = (!cells.NullOrEmpty()) ? cells[j].GetThingList(mapHeld) : null;
            //                var thingCount = (!thingList.NullOrEmpty()) ? thingList.Count : 0;
            //                for (int i = 0; i < thingCount; i++)
            //                {
            //                    if (thingList[i] is Pawn p && !(thingList[i] is PawnWebSpinner))
            //                    {
            //                        WebEffect(p);
            //                    }
            //                }
            //            }
            //            cells = null;
            //        }
            //    }
            //}
            //catch (Exception) { }
        }

        public override string GetInspectString()
        {
            var str2 = "None".Translate();
            var compDisappearsStr = this.GetComp<CompLifespan>()?.CompInspectStringExtra()?.TrimEndNewlines() ?? "";
            var result = new StringBuilder();

            if (Spinner != null)
            {
                str2 = (spinner.Faction != Faction.OfPlayerSilentFail) ? "ROM_Wild".Translate(spinner.Label) : spinner.Name.ToStringFull;
                if (spinner.Dead) str2 = "DeadLabel".Translate(str2);
            }

            result.AppendLine("ROM_Spinner".Translate() + ": " + str2);
            result.AppendLine(compDisappearsStr);

            return result.ToString().TrimEndNewlines();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<PawnWebSpinner>(ref this.spinner, "spinner");
        }
    }
}
