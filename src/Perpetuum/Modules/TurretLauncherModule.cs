using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Players;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Finders.PositionFinders;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.SentryTurrets;
using Perpetuum.Zones;
using System;
using System.Linq;
using Perpetuum.Zones.Finders;
using Perpetuum.Units;

namespace Perpetuum.Modules
{
    public class TurretLauncherModule : ActiveModule
    {
        private const int SentryTurretHeight = 7;
        private const double SentryTurretDeployRange = 2;
                
        public TurretLauncherModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags, true)
        {
            optimalRange.AddEffectModifier(AggregateField.effect_ew_optimal_range_modifier);
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
            var zone = Zone;

            if (zone == null)
            {
                return;
            }

            Position? lockPosition;

            var myLock = GetLock();

            if (myLock is TerrainLock)
            {
                lockPosition = (myLock as TerrainLock).Location.AddToZ(SentryTurretHeight);
            }
            else if (myLock is UnitLock)
            {
                lockPosition = (myLock as UnitLock).Target.CurrentPosition.AddToZ(SentryTurretHeight);
            }
            else
            {
                OnError(ErrorCodes.InvalidLockType);

                return;
            }

            Position targetPosition = lockPosition.Value;
            zone.Units.OfType<SentryTurret>().WithinRange(targetPosition, SentryTurretDeployRange).Any().ThrowIfTrue(ErrorCodes.BlobEmitterInRange);
            
            var r = zone.IsInLineOfSight(ParentRobot, targetPosition, false);

            if (r.hit)
            {
                OnError(ErrorCodes.LOSFailed);

                return;
            }

            var ammo = GetAmmo();
            var fieldTurret = (SentryTurret)Factory.CreateWithRandomEID(ammo.ED.Options.TurretId);

            ParentRobot.HasFreeBandwidthOf(fieldTurret).ThrowIfFalse(ErrorCodes.MaxBandwidthExceed);
            ParentRobot.UseRemoteChannel(fieldTurret);

            var despawnTimeMod = ammo.GetPropertyModifier(AggregateField.despawn_time);

            fieldTurret.DespawnTime = TimeSpan.FromMilliseconds(despawnTimeMod.Value);
            
            var finder = new ClosestWalkablePositionFinder(zone, targetPosition);
            var position = finder.FindOrThrow();
            var beamBuilder = Beam.NewBuilder()
                .WithType(BeamType.deploy_device_01)
                .WithPosition(targetPosition)
                .WithDuration(TimeSpan.FromSeconds(5));

            fieldTurret.AddToZone(zone, position, ZoneEnterType.Default, beamBuilder);

            ConsumeAmmo();
        }
    }
}
