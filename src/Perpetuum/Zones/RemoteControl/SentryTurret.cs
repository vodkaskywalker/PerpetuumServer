using Perpetuum.EntityFramework;
using Perpetuum.Players;
using Perpetuum.Services.Standing;
using Perpetuum.Units;
using Perpetuum.Zones.DamageProcessors;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.NpcSystem.ThreatManaging;
using System;

namespace Perpetuum.Zones.RemoteControl
{
    public class SentryTurret : RemoteControlledCreature
    {
        public SentryTurret(IStandingHandler standingHandler)
            : base(standingHandler)
        {
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        protected override bool IsHostileFor(Unit unit)
        {
            return unit.IsHostile(this);
        }

        public override bool IsHostile(Player targetPlayer)
        {
            return IsHostilePlayer(targetPlayer);
        }

        public override void OnAggression(Unit victim)
        {
            CommandRobot.OnAggression(victim);
        }

        internal override bool IsHostile(Npc npc)
        {
            return true;
        }

        internal override bool IsHostile(CombatDrone drone)
        {
            return !(drone.CommandRobot is Player player) || IsHostilePlayer(player);
        }

        internal override bool IsHostile(SentryTurret turret)
        {
            return !(turret.CommandRobot is Player player) || IsHostilePlayer(player);
        }

        protected override void UpdateUnitVisibility(Unit target)
        {
            if (target is Npc ||
                target is RemoteControlledCreature)
            {
                UpdateVisibility(target);
            }
        }

        protected override void OnUpdate(TimeSpan time)
        {
            if (!IsInOperationalRange)
            {
                Kill();
            }

            base.OnUpdate(time);
        }

        protected override void OnDamageTaken(Unit source, DamageTakenEventArgs e)
        {
            base.OnDamageTaken(source, e);

            Player player = Zone.ToPlayerOrGetOwnerPlayer(source);

            if (player == null)
            {
                return;
            }

            if (IsHostilePlayer(player))
            {
                AddThreat(player, new Threat(ThreatType.Damage, e.TotalDamage * 0.9), true);
            }
        }
    }
}