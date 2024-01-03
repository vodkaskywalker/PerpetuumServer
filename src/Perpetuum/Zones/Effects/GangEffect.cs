using System.Collections.Generic;
using System.Linq;
using Perpetuum.Groups.Gangs;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones.RemoteControl;

namespace Perpetuum.Zones.Effects
{
    /// <summary>
    /// Gang based effect
    /// </summary>
    public class GangEffect : AuraEffect
    {
        protected override void OnTick()
        {
            if (Owner != Source)
            {
                var gang = Owner is Player player
                    ? player.Gang
                    : Owner is RemoteControlledTurret turret
                        ? turret.Player.Gang
                        : null;

                if (gang == null || !gang.IsMember((Player)Source))
                {
                    OnRemoved();

                    return;
                }

            }

            base.OnTick();
        }

        protected override IEnumerable<Unit> GetTargets(IZone zone)
        {
            var player = (Player)Owner;
            var gangMembers = zone.GetGangMembers(player.Gang);
            var alliedTurrets = zone.GetAlliedTurretsByPlayers(new[] { player }.Union(gangMembers));
            var alliedUnits = (gangMembers).Union<Unit>(alliedTurrets);

            return alliedUnits.WithinRange(Owner.CurrentPosition, Radius);
        }

    }
}