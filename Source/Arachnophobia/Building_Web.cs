﻿using System;
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
            float num = 20f;
            float num2 = Mathf.Lerp(0.85f, 1.15f, p.thingIDNumber ^ 74374237);
            num *= num2;
            p.TakeDamage(new DamageInfo(DamageDefOf.Stun, (int)num, -1, this));
            spinner.Notify_WebTouched(p);
            if (p?.Faction == Faction.OfPlayerSilentFail) Messages.Message("ROM_SpiderWebsCrossed".Translate(p.LabelShort), MessageSound.Standard);
            spinner.Web = null;
            if (Rand.Value > 0.9) this.Destroy(DestroyMode.KillFinalize);
            else this.Destroy(DestroyMode.Vanish);
        }

        public override void Tick()
        {
            base.Tick();
            try
            {

                if (!this.Destroyed && this.Spawned && Find.TickManager.TicksGame % 20 == 0)
                {
                    var mapHeld = this.MapHeld;
                    if (mapHeld != null)
                    {
                        var cells = new List<IntVec3>(this?.OccupiedRect().Cells?.ToList() ?? null);
                        var cellCount = (!cells.NullOrEmpty()) ? cells.Count : 0;
                        for (int j = 0; j < cellCount; j++)
                        {
                            var thingList = (!cells.NullOrEmpty()) ? cells[j].GetThingList(mapHeld) : null;
                            var thingCount = (!thingList.NullOrEmpty()) ? thingList.Count : 0;
                            for (int i = 0; i < thingCount; i++)
                            {
                                if (thingList[i] is Pawn p && !(thingList[i] is PawnWebSpinner))
                                {
                                    WebEffect(p);
                                }
                            }
                        }
                        cells = null;
                    }
                }
            }
            catch (Exception) { }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<PawnWebSpinner>(ref this.spinner, "spinner");
        }
    }
}