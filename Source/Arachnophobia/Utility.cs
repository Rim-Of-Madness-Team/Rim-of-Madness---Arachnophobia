using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Arachnophobia
{
    public static class Utility
    {
        public static List<Thing> CocoonsFor(Map map, Thing t)
        {
            var result = new List<Thing>();
            bool playerCocoonHandler(Building_Cocoon y) => (!y.IsInAnyStorage() || (t.Faction == Faction.OfPlayerSilentFail));
            result = map?.listerThings?.AllThings?.FindAll(x => x is Building_Cocoon y && playerCocoonHandler(y) && (y.Spinner == null || y.Spinner?.def == t.def)) ?? null;
            return result;
        }


    }
}
