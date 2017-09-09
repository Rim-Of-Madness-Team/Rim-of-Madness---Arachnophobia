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
        public static HashSet<Thing> CocoonsFor(Map map, Thing t)
        {
            //All cocoons in the allowed area for Thing t.
            //var allCocoons = new List<Thing>(map.GetComponent<MapComponent_CocoonTracker>().AllCocoons);

            //Wild spiders should go for non-home located cocoons and cocoons that are not in storage areas.
            var wildCocoons = map.GetComponent<MapComponent_CocoonTracker>().WildCocoons;
            if ((wildCocoons != null || wildCocoons.Count > 0)  && t.Faction != Faction.OfPlayerSilentFail) return wildCocoons;

            //Domestic spiders should go for home located cocoons or cocoons in storage areas.
            var domesticCocoons = map.GetComponent<MapComponent_CocoonTracker>().DomesticCocoons;
            if ((domesticCocoons != null || domesticCocoons.Count > 0) && t.Faction == Faction.OfPlayerSilentFail) return new HashSet<Thing>(domesticCocoons.Where(x => ForbidUtility.InAllowedArea(x.PositionHeld, t as Pawn)));

            //Other cases should not exist.
            //("Arachophobia :: No cocoons exist");
            return null;
        }

        public static Thing DetermineBestCocoon(List<Thing> cocoons, PawnWebSpinner spinner)
        {
            Thing result = null;
            if (cocoons != null && cocoons.Count > 0)
                result = GenClosest.ClosestThingReachable(spinner.Position, spinner.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), Verse.AI.PathEndMode.ClosestTouch,
                TraverseParms.For(spinner, Danger.Deadly), 9999, (x => x is Building_Cocoon y && cocoons.Contains(y) && y.isConsumableBy(spinner)));
            return result;
        }


    }
}
