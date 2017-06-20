﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;
using RimWorld;

namespace Arachnophobia
{
    public class JobDriver_SpinWeb : JobDriver
    {
        private const TargetIndex LaySpotInd = TargetIndex.A;

        private ThingDef WebDef
        {
            get
            {
                var result = ROMADefOf.ROMA_Web;
                if ((this?.pawn?.RaceProps?.baseBodySize ?? 0) > 2) result = ROMADefOf.ROMA_WebGiant;
                return result;
            }
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = 500,
                initAction = delegate
                {
                    int i = 999;
                    while (i > 0)
                    {
                        this.pawn.CurJob.SetTarget(TargetIndex.A, RCellFinder.RandomWanderDestFor(this.pawn, this.pawn.Position, 5f, null, Danger.Some));
                        if (GenConstruct.CanPlaceBlueprintAt(WebDef, TargetLocA, Rot4.North, this.pawn.Map).Accepted)
                        {
                            break;
                        }
                        i--;
                    }
                }
            };
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            yield return new Toil
            {
                initAction = delegate
                {
                    var spinner = this.GetActor() as PawnWebSpinner;
                    if (spinner != null)
                    {
                        var web = (Building_Web)GenSpawn.Spawn(WebDef, spinner.Position, spinner.Map);
                        spinner.Web = web;
                        spinner.WebsMade++;
                        web.Spinner = spinner;
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
