using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones.NpcSystem;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.Effects
{
    public class NoxEffect : AuraEffect
    {
        protected override IEnumerable<Unit> GetTargets(IZone zone)
        {
            IEnumerable<Unit> affectedUnits = zone.GetUnitsWithinRange2D(Owner.CurrentPosition, Radius);
            if (Owner is Npc npc)
            {
                affectedUnits = affectedUnits.Where(u => u is Player);
            }
            else
            {
                if (zone.Configuration.IsAlpha)
                {
                    affectedUnits = affectedUnits.Where(u => u is Npc);
                }
            }

            return affectedUnits;
        }

    }
}
