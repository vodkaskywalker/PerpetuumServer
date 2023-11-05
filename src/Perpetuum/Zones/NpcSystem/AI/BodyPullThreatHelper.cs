using Perpetuum.EntityFramework;
using Perpetuum.Modules.Weapons;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones.Eggs;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class BodyPullThreatHelper : IEntityVisitor<Player>, IEntityVisitor<AreaBomb>, IEntityVisitor<Npc>
    {
        private readonly SmartCreature smartCreature;

        public BodyPullThreatHelper(SmartCreature smartCreature)
        {
            this.smartCreature = smartCreature;
        }

        public void Visit(Player player)
        {
            if (smartCreature.Behavior.Type != BehaviorType.Aggressive &&
                smartCreature.Behavior.Type != BehaviorType.RemoteControlled &&
                !player.HasRemoteControlEffect)
            {
                return;
            }

            if (player.HasTeleportSicknessEffect)
            {
                return;
            }

            if (smartCreature.ThreatManager.Hostiles.Any(h => h.unit.Eid == player.Eid))
            {
                return;
            }

            if (!smartCreature.IsInAggroRange(player))
            {
                return;
            }

            var threat = Threat.BODY_PULL + FastRandom.NextDouble(0, 5);

            smartCreature.AddThreat(player, new Threat(ThreatType.Bodypull, threat));
        }

        public void Visit(AreaBomb bomb)
        {
            if (!smartCreature.IsInAggroRange(bomb))
            {
                return;
            }

            if (!smartCreature.ActiveModules.Any(m => m is WeaponModule))
            {
                return;
            }

            var group = smartCreature.Group;

            if (group != null && group.Members.Any(m => m.ThreatManager.Contains(bomb)))
            {
                return;
            }

            var threat = Threat.BODY_PULL;

            if (smartCreature.ThreatManager.IsThreatened)
            {
                var h = smartCreature.ThreatManager.GetMostHatedHostile();

                if (h != null)
                {
                    threat = h.Threat * 100;
                }
            }

            smartCreature.AddThreat(bomb, new Threat(ThreatType.Bodypull, threat + FastRandom.NextDouble(0, 5)));
        }

        public void Visit(Npc npc)
        {
            if (smartCreature.Behavior.Type != BehaviorType.RemoteControlled)
            {
                return;
            }

            if (!smartCreature.ActiveModules.Any(m => m is WeaponModule))
            {
                return;
            }

            if (smartCreature.ThreatManager.Hostiles.Any(h => h.unit.Eid == npc.Eid))
            {
                return;
            }

            if (!smartCreature.IsInAggroRange(npc))
            {
                return;
            }

            var threat = Threat.BODY_PULL + FastRandom.NextDouble(0, 5);

            smartCreature.AddThreat(npc, new Threat(ThreatType.Bodypull, threat));
        }
    }
}
