using RimWorld;
using System;
using Verse;

namespace Arachnophobia
{
    internal class IncidentWorker_GiantSpiders : IncidentWorker
    {
        public override bool TryExecute(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            var giantSpider = ROMADefOf.ROMA_SpiderKindGiant;
            var giantSpiderQueen = ROMADefOf.ROMA_SpiderKindGiantQueen;
            IntVec3 intVec;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out intVec, map, CellFinder.EdgeRoadChance_Animal, null))
            {
                return false;
            }
            int num = Rand.RangeInclusive(3, 6);
            IntVec3 loc = CellFinder.RandomClosewalkCellNear(intVec, map, 10, null);
            for (int i = 0; i < num; i++)
            {
                Pawn newThing = PawnGenerator.GeneratePawn(giantSpider, null);
                loc = CellFinder.RandomClosewalkCellNear(intVec, map, 10, null);
                GenSpawn.Spawn(newThing, loc, map);
            }
            loc = CellFinder.RandomClosewalkCellNear(intVec, map, 10, null);
            Pawn spiderQueen = PawnGenerator.GeneratePawn(giantSpiderQueen);
            GenSpawn.Spawn(spiderQueen, loc, map);
            Find.LetterStack.ReceiveLetter("ROM_LetterLabelSpidersArrived".Translate(), "ROM_SpidersArrived".Translate(), LetterDefOf.BadNonUrgent, new TargetInfo(intVec, map, false), null);
            return true;
        }
    }
}
