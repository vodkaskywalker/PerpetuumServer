using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Modules.Weapons;
using Perpetuum.Robots;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.NpcSystem.ThreatManaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Perpetuum.Modules
{
    public class ScorcherModule : EnergyDispersionModule
    {
        private const int AffectedTargetsDepth = 5;
        private List<Unit> afectedTargets;
        private readonly ItemProperty electricDamage;

        public ScorcherModule()
        {
            electricDamage = new ModuleProperty(this, AggregateField.electric_damage);
            AddProperty(electricDamage);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        protected override void OnAction()
        {
            UnitLock unitLock = GetLock().ThrowIfNotType<UnitLock>(ErrorCodes.InvalidLockType);

            if (!LOSCheckAndCreateBeam(unitLock.Target))
            {
                OnError(ErrorCodes.LOSFailed);

                return;
            }

            afectedTargets = GetAffectedTargetsRecursively(unitLock.Target, AffectedTargetsDepth);
            Robot chainedRobot = ParentRobot;

            foreach (Unit target in afectedTargets)
            {
                BeamBuilder deployBeamBuilder = Beam.NewBuilder()
                    .WithType(BeamType.medium_e_nezt_beam)
                    .WithSource(chainedRobot)
                    .WithTarget(target)
                    .WithState(BeamState.Hit)
                    .WithDuration(TimeSpan.FromSeconds(5));
                Zone.CreateBeam(deployBeamBuilder);
                double coreNeutralized = electricDamage.Value;
                double coreNeutralizedDone = 0.0;
                ModifyValueByReactorRadiation(target, ref coreNeutralized);
                coreNeutralized = ModifyValueByOptimalRange(chainedRobot, target, coreNeutralized);
                if (coreNeutralized > 0.0)
                {
                    double core = target.Core;

                    target.Core -= coreNeutralized;
                    coreNeutralizedDone = Math.Abs(core - target.Core);
                    target.OnCombatEvent(ParentRobot, new EnergyDispersionEventArgs(coreNeutralizedDone));

                    double threatValue = (coreNeutralizedDone / 2) + 1;

                    target.AddThreat(ParentRobot, new Threat(ThreatType.EnWar, threatValue));
                }

                IDamageBuilder builder = GetDamageBuilder(coreNeutralizedDone);
                _ = Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(t => target.TakeDamage(builder.Build()));
                CombatLogPacket packet = new CombatLogPacket(CombatLogType.EnergyNeutralize, target, ParentRobot, this);
                packet.AppendDouble(coreNeutralized);
                packet.AppendDouble(coreNeutralizedDone);
                packet.Send(target, ParentRobot);
                chainedRobot = (Robot)target;
            }
        }

        private List<Unit> GetAffectedTargetsRecursively(Unit target, int depth)
        {
            List<Unit> targets = new List<Unit> { target };
            if (depth <= 0)
            {
                return targets;
            }

            Unit unit = target.Zone
                .GetUnitsWithinRange2D(target.CurrentPosition, OptimalRange)
                .OfType<Robot>()
                .Except(targets)
                .Where(x => x.Owner != Owner && Zone.IsInLineOfSight(target.CurrentPosition, x, false).hit)
                .GetNearestUnit(target.CurrentPosition);
            if (unit != null)
            {
                targets.AddRange(GetAffectedTargetsRecursively(unit, depth - 1));
            }

            return targets;
        }

        private IDamageBuilder GetDamageBuilder(double damageValue)
        {
            return DamageInfo.Builder
                .WithAttacker(ParentRobot)
                .WithOptimalRange(OptimalRange)
                .WithFalloff(Falloff)
                .WithDamages(new[] { new Damage(DamageType.Electric, damageValue) });
        }
    }
}
