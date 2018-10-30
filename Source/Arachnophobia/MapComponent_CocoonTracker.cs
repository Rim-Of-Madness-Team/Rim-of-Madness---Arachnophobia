using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Arachnophobia
{
    public class MapComponent_CocoonTracker : MapComponent
    {
        public bool isSpiderPair = false;
        public bool isGiantSpiderPair = false;

        private HashSet<Thing> wildCocoons;
        private HashSet<Thing> domesticCocoons;

        public HashSet<Thing> AllCocoons => new HashSet<Thing>(WildCocoons?.Concat(DomesticCocoons)?.ToList() ?? null);
        public HashSet<Thing> WildCocoons
        {
            get
            {
                if (wildCocoons == null)
                {
                    wildCocoons = new HashSet<Thing>(map?.listerThings?.AllThings?.FindAll(x => x is Building_Cocoon y && y.Spawned && (!x.Map?.areaManager?.Home[x.Position] ?? false) && (!x?.IsInAnyStorage() ?? false)));
                }
                return wildCocoons;
            }
        }

        public HashSet<Thing> DomesticCocoons
        {
            get
            {
                if (domesticCocoons == null)
                {
                    List<Thing> allTemp = map?.listerThings?.AllThings?.FindAll(x => x is Building_Cocoon y && y.Spawned);
                    domesticCocoons = new HashSet<Thing>(allTemp.FindAll(x => (x.Map?.areaManager?.Home[x.Position] ?? false) || (x?.IsInAnyStorage() ?? false)));
                }
                return domesticCocoons;
            }
        }
        //Wild spiders should go for non-home located cocoons and cocoons that are not in storage areas.
        //var wildCocoons = new List<Thing>(


        //    //Domestic spiders should go for home located cocoons or cocoons in storage areas.
        //    var domesticCocoons = new List<Thing>(
        //        allCocoons?.FindAll(x => (x.Map?.areaManager?.Home[x.Position] ?? false) || (x?.IsInAnyStorage() ?? false))
        //        );
        //    if (domesticCocoons != null && domesticCocoons.Count > 0 && t.Faction == Faction.OfPlayerSilentFail) return domesticCocoons;

        //    //Other cases should not exist.
        //    //("Arachophobia :: No cocoons exist");
        //    return allCocoons;

        public MapComponent_CocoonTracker(Map map) : base(map)
        {

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref isSpiderPair, "isSpiderPair", false);
            Scribe_Values.Look(ref isGiantSpiderPair, "isGiantSpiderPair", false);
        }
    }
}
