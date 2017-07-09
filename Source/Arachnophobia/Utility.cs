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
        public static List<Thing> CocoonsFor(Map map, Thing t, Building_Cocoon exception = null)
        {
            //All cocoons in the allowed area for Thing t.
            var allCocoons = new List<Thing>(
                map?.listerThings?.AllThings?.FindAll(x => x is Building_Cocoon y && y.Spawned && x.Position.InAllowedArea((Pawn)t) && y != exception)
                );
            
            //Wild spiders should go for non-home located cocoons and cocoons that are not in storage areas.
            var wildCocoons = new List<Thing>(
                allCocoons?.FindAll(x => (!x.Map?.areaManager?.Home[x.Position] ?? false) && (!x?.IsInAnyStorage() ?? false))
                );
            if (wildCocoons != null && wildCocoons.Count > 0 && t.Faction != Faction.OfPlayerSilentFail) return wildCocoons;

            //Domestic spiders should go for home located cocoons or cocoons in storage areas.
            var domesticCocoons = new List<Thing>(
                allCocoons?.FindAll(x => (x.Map?.areaManager?.Home[x.Position] ?? false) || (x?.IsInAnyStorage() ?? false))
                );
            if (domesticCocoons != null && domesticCocoons.Count > 0 && t.Faction == Faction.OfPlayerSilentFail) return domesticCocoons;

            //Other cases should not exist.
            Log.Message("Arachophobia :: No cocoons exist");
            return allCocoons;
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
