using Perpetuum.EntityFramework;
using Perpetuum.Modules.Weapons;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones.Eggs;
using Perpetuum.Zones.NpcSystem.AI.Behaviors;
using Perpetuum.Zones.NpcSystem.ThreatManaging;
using Perpetuum.Zones.RemoteControl;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class BodyPullThreatHelper :
        IEntityVisitor<Player>,
        IEntityVisitor<AreaBomb>,
        IEntityVisitor<Npc>,
        IEntityVisitor<SentryTurret>,
        IEntityVisitor<CombatDrone>
    {
        private readonly SmartCreature smartCreature;

        public BodyPullThreatHelper(SmartCreature smartCreature)
        {
            this.smartCreature = smartCreature;
        }

        public void Visit(Player player)
        {
            if (smartCreature.Behavior.Type != BehaviorType.Aggressive &&
                smartCreature.Behavior.Type != BehaviorType.RemoteControlledTurret &&
                smartCreature.Behavior.Type != BehaviorType.RemoteControlledDrone)
            {
                return;
            }

            if (player.HasTeleportSicknessEffect)
            {
                return;
            }

            if (smartCreature is RemoteControlledCreature remoteControlledCreature &&
                player.Zone.Configuration.IsAlpha &&
                !player.HasPvpEffect &&
                !remoteControlledCreature.Player.HasPvpEffect)
            {
                return;
            }

            if (smartCreature.ThreatManager.Hostiles.Any(h => h.Unit.Eid == player.Eid))
            {
                return;
            }

            if (!smartCreature.IsInAggroRange(player))
            {
                return;
            }

            double threat = Threat.BODY_PULL + FastRandom.NextDouble(0, 5);

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

            Flocks.ISmartCreatureGroup group = smartCreature.Group;

            if (group != null && group.Members.Any(m => m.ThreatManager.Contains(bomb)))
            {
                return;
            }

            double threat = Threat.BODY_PULL;

            if (smartCreature.ThreatManager.IsThreatened)
            {
                Hostile h = smartCreature.ThreatManager.GetMostHatedHostile();

                if (h != null)
                {
                    threat = h.Threat * 100;
                }
            }

            smartCreature.AddThreat(bomb, new Threat(ThreatType.Bodypull, threat + FastRandom.NextDouble(0, 5)));
        }

        public void Visit(Npc npc)
        {
            if (smartCreature.Behavior.Type != BehaviorType.RemoteControlledTurret &&
                smartCreature.Behavior.Type != BehaviorType.RemoteControlledDrone)
            {
                return;
            }

            if (!smartCreature.ActiveModules.Any(m => m is WeaponModule))
            {
                return;
            }

            if (smartCreature.ThreatManager.Hostiles.Any(h => h.Unit.Eid == npc.Eid))
            {
                return;
            }

            if (!smartCreature.IsInAggroRange(npc))
            {
                return;
            }

            double threat = Threat.BODY_PULL + FastRandom.NextDouble(0, 5);

            smartCreature.AddThreat(npc, new Threat(ThreatType.Bodypull, threat));
        }

        public void Visit(SentryTurret sentryTurret)
        {
            if (smartCreature.Behavior.Type != BehaviorType.RemoteControlledTurret &&
                smartCreature.Behavior.Type != BehaviorType.RemoteControlledDrone)
            {
                return;
            }

            if (!smartCreature.ActiveModules.Any(m => m is WeaponModule))
            {
                return;
            }

            if (smartCreature.ThreatManager.Hostiles.Any(h => h.Unit.Eid == sentryTurret.Eid))
            {
                return;
            }

            if (!smartCreature.IsInAggroRange(sentryTurret))
            {
                return;
            }

            double threat = Threat.BODY_PULL + FastRandom.NextDouble(0, 5);

            smartCreature.AddThreat(sentryTurret, new Threat(ThreatType.Bodypull, threat));
        }

        public void Visit(CombatDrone combatDrone)
        {
            if (smartCreature.Behavior.Type != BehaviorType.RemoteControlledTurret &&
                smartCreature.Behavior.Type != BehaviorType.RemoteControlledDrone)
            {
                return;
            }

            if (!smartCreature.ActiveModules.Any(m => m is WeaponModule))
            {
                return;
            }

            if (smartCreature.ThreatManager.Hostiles.Any(h => h.Unit.Eid == combatDrone.Eid))
            {
                return;
            }

            if (!smartCreature.IsInAggroRange(combatDrone))
            {
                return;
            }

            double threat = Threat.BODY_PULL + FastRandom.NextDouble(0, 5);

            smartCreature.AddThreat(combatDrone, new Threat(ThreatType.Bodypull, threat));
        }
    }
}
