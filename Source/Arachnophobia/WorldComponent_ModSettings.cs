using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using RimWorld.Planet;

namespace Arachnophobia
{
    public class WorldComponent_ModSettings : WorldComponent
    {
        private bool spiderDefsModified = false;
        public bool SpiderDefsModified { get { return spiderDefsModified; } set { spiderDefsModified = value; } }
        public WorldComponent_ModSettings(World world) : base(world)
        {
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            ResolveSpiderDefSettings();
        }

        public void ResolveSpiderDefSettings()
        {
            if (!SpiderDefsModified)
            {
                Log.Message("Arachnophobia :: Spider Biome Settings Adjusted :: Current Factor: " + ModInfo.romSpiderFactor);
                SpiderDefsModified = true;

                List<ThingDef> spiders = 
                    new List<ThingDef>
                    {
                        ROMADefOf.ROMA_SpiderRace,
                        ROMADefOf.ROMA_SpiderRaceGiant
                    };

                List<PawnKindDef> spiderKinds =
                    new List<PawnKindDef>
                    {
                        ROMADefOf.ROMA_SpiderKind,
                        ROMADefOf.ROMA_SpiderKindGiant
                    };

                foreach (ThingDef def in spiders)
                {
                    foreach (AnimalBiomeRecord record in def.race.wildBiomes)
                    {
                        record.commonality *= ModInfo.romSpiderFactor;
                    }
                }

                foreach (PawnKindDef kind in spiderKinds)
                {
                    kind.wildSpawn_EcoSystemWeight *= ModInfo.romSpiderFactor;
                }
            }
        }
    }
}
