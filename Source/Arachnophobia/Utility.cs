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
        public static List<Thing> Cocoons(Map map, ThingDef raceDef)
        {
            var result = new List<Thing>();
            result = map?.listerThings?.AllThings?.FindAll(x => x is Building_Cocoon y && y.Spinner?.def == raceDef) ?? null;
            return result;
        }
    }
}
