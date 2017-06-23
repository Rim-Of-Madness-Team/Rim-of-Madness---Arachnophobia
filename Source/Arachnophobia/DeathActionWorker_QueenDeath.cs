using System;
using Verse;
using RimWorld;

namespace Arachnophobia
{
    public class DeathActionWorker_QueenDeath : DeathActionWorker
    {
        public override void PawnDied(Corpse corpse)
        {
            var hostFaction = corpse?.Faction;
            if (hostFaction != Faction.OfPlayerSilentFail)
            {
                for (int i = 0; i < Rand.Range(60, 120); i++)
                {
                    var newPawn = PawnGenerator.GeneratePawn(ROMADefOf.ROMA_SpiderKind);
                    newPawn.ageTracker.AgeBiologicalTicks = 0;
                    var newThing = GenSpawn.Spawn(newPawn, corpse.Position, corpse.Map);
                    if (hostFaction != null) newThing.SetFaction(hostFaction);
                }
                Messages.Message("ROM_SpiderQueenDeath", MessageSound.SeriousAlert);
            }
        }
    }
}
