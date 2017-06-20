using System;
using Verse;
using RimWorld;

namespace Arachnophobia
{
    public class CompProperties_MultiHatcher : CompProperties
    {
        public float hatcherDaystoHatch = 1f;

        public PawnKindDef hatcherPawn;

        public IntRange hatcherNumber = new IntRange(2, 3);

        public CompProperties_MultiHatcher()
        {
            this.compClass = typeof(CompMultiHatcher);
        }
    }
}
