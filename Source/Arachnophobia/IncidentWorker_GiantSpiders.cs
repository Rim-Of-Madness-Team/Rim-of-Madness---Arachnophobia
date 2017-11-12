using RimWorld;
using System;
using Verse;

namespace Arachnophobia
{
    internal class IncidentWorker_GiantSpiders : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            var giantSpider = ROMADefOf.ROMA_SpiderKindGiant;
            var giantSpiderQueen = ROMADefOf.ROMA_SpiderKindGiantQueen;
            bool queenSpawned = false;
            string desc = "ROM_SpidersArrivedNoQueen";
            IntVec3 intVec;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out intVec, map, CellFinder.EdgeRoadChance_Animal, null))
            {
                return false;
            }
            float points = parms.points;
            int num = Rand.RangeInclusive(3, 6);
            IntVec3 loc = CellFinder.RandomClosewalkCellNear(intVec, map, 10, null);
            for (int i = 0; i < num; i++)
            {
                Pawn newThing = PawnGenerator.GeneratePawn(giantSpider, null);
                loc = CellFinder.RandomClosewalkCellNear(intVec, map, 10, null);
                GenSpawn.Spawn(newThing, loc, map);
                points -= giantSpider.combatPower;
            }
            loc = CellFinder.RandomClosewalkCellNear(intVec, map, 10, null);
            if (points > giantSpiderQueen.combatPower)
            {
                queenSpawned = true;
                Pawn spiderQueen = PawnGenerator.GeneratePawn(giantSpiderQueen);
                GenSpawn.Spawn(spiderQueen, loc, map);
            }
            //ROM_SpidersArrived
            if (queenSpawned) desc = "ROM_SpidersArrived";
            Find.LetterStack.ReceiveLetter("ROM_LetterLabelSpidersArrived".Translate(), desc.Translate(), LetterDefOf.ThreatSmall, new TargetInfo(intVec, map, false), null);
            return true;
        }
    }
}
