using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Finders;
using Perpetuum.Zones.Finders.PositionFinders;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.NpcSystem.AI.Behaviors;
using Perpetuum.Zones.RemoteControl;
using Perpetuum.Zones.Teleporting;
using System;
using System.Linq;

namespace Perpetuum.Modules
{
    public class RemoteControllerModule : ActiveModule
    {
        private const int SentryTurretHeight = 7;
        private const double SpawnRangeMin = 2;
        private const double SpawnRangeMax = 5;
        private readonly ModuleProperty bandwidthMax;
        private readonly ModuleProperty operationalRange;
        private readonly ModuleProperty lifetime;
        private BandwidthHandler bandwidthHandler;

        public double BandwidthMax => bandwidthMax.Value;

        public RemoteControllerModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags, true)
        {
            optimalRange.AddEffectModifier(AggregateField.effect_ew_optimal_range_modifier);

            bandwidthMax = new ModuleProperty(this, AggregateField.remote_control_bandwidth_max);
            AddProperty(bandwidthMax);

            operationalRange = new ModuleProperty(this, AggregateField.remote_control_operational_range);
            AddProperty(operationalRange);

            lifetime = new ModuleProperty(this, AggregateField.remote_control_lifetime);
            AddProperty(lifetime);

            InitBandwidthHandler(this);
        }

        private void InitBandwidthHandler(RemoteControllerModule module)
        {
            bandwidthHandler = new BandwidthHandler(module);
        }

        public void SyncRemoteChannels()
        {
            bandwidthHandler.Update();
        }

        public bool HasFreeBandwidthFor(RemoteControlledUnit unit)
        {
            return bandwidthHandler.HasFreeBandwidthFor(unit);
        }

        public void UseRemoteChannel(RemoteControlledCreature turret)
        {
            bandwidthHandler.UseRemoteChannel(turret);
            turret.RemoteChannelDeactivated += bandwidthHandler.OnRemoteChannelDeactivated;
            bandwidthHandler.Update();
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public void CloseAllChannels()
        {
            bandwidthHandler.CloseAllChannels();
        }

        protected override void OnAction()
        {
            if (bandwidthHandler == null)
            {
                return;
            }

            SyncRemoteChannels();

            RemoteControlledUnit ammo = GetAmmo() as RemoteControlledUnit;

            HasFreeBandwidthFor(ammo).ThrowIfFalse(ErrorCodes.MaxBandwidthExceed);

            Position targetPosition;

            if (ammo.ED.Options.TurretType != TurretType.CombatDrone)
            {

                Lock myLock = GetLock();

                if (myLock is TerrainLock)
                {
                    targetPosition = (myLock as TerrainLock).Location;
                }
                else if (myLock is UnitLock)
                {
                    targetPosition = (myLock as UnitLock).Target.CurrentPosition;
                }
                else
                {
                    OnError(ErrorCodes.InvalidLockType);

                    return;
                }

                Position spawnPosition = Zone.FixZ(targetPosition);

                Zone.Units
                    .OfType<RemoteControlledCreature>()
                    .WithinRange(spawnPosition, DistanceConstants.RCU_DEPLOY_RANGE_FROM_RCU)
                    .Any()
                    .ThrowIfTrue(ErrorCodes.RemoteControlledTurretInRange);

                Zone.Units
                    .OfType<DockingBase>()
                    .WithinRange(spawnPosition, DistanceConstants.RCU_DEPLOY_RANGE_FROM_BASE)
                    .Any()
                    .ThrowIfTrue(ErrorCodes.NotDeployableNearObject);

                Zone.Units
                    .OfType<Teleport>()
                    .WithinRange(spawnPosition, DistanceConstants.RCU_DEPLOY_RANGE_FROM_TELEPORT)
                    .Any()
                    .ThrowIfTrue(ErrorCodes.TeleportIsInRange);

                LOSResult r = Zone.IsInLineOfSight(ParentRobot, targetPosition.AddToZ(SentryTurretHeight), false);

                if (r.hit)
                {
                    OnError(ErrorCodes.LOSFailed);

                    return;
                }
            }
            else
            {
                targetPosition = GetSpawnPosition(ParentRobot.CurrentPosition);
            }

            Player player = ParentRobot is Player robotAsPlayer
                ? robotAsPlayer
                : null;


            if (player != null)
            {
                ammo.CheckEnablerExtensionsAndThrowIfFailed(player.Character, ErrorCodes.ExtensionLevelMismatchTerrain);
            }

            RemoteControlledCreature remoteControlledCreature;
            if (ammo.ED.Options.TurretType == TurretType.Sentry)
            {
                remoteControlledCreature = (SentryTurret)Factory.CreateWithRandomEID(ammo.ED.Options.TurretId);
                remoteControlledCreature.Behavior = Behavior.Create(BehaviorType.RemoteControlledTurret);
            }
            else if (ammo.ED.Options.TurretType == TurretType.Mining || ammo.ED.Options.TurretType == TurretType.Harvesting)
            {
                remoteControlledCreature = (IndustrialTurret)Factory.CreateWithRandomEID(ammo.ED.Options.TurretId);
                (remoteControlledCreature as IndustrialTurret).SetTurretType(ammo.ED.Options.TurretType);
                remoteControlledCreature.Behavior = Behavior.Create(BehaviorType.RemoteControlledTurret);
            }
            else if (ammo.ED.Options.TurretType == TurretType.CombatDrone)
            {
                remoteControlledCreature = (CombatDrone)Factory.CreateWithRandomEID(ammo.ED.Options.TurretId);
                remoteControlledCreature.Behavior = Behavior.Create(BehaviorType.RemoteControlledDrone);
                (remoteControlledCreature as CombatDrone).GuardRange = 5;
            }
            else
            {
                _ = PerpetuumException.Create(ErrorCodes.InvalidAmmoDefinition);

                return;
            }

            remoteControlledCreature.SetCommandRobot(player ?? ParentRobot);

            remoteControlledCreature.Owner = ParentRobot.Owner;
            remoteControlledCreature.SetBandwidthUsage(ammo.RemoteChannelBandwidthUsage);

            UseRemoteChannel(remoteControlledCreature);

            remoteControlledCreature.DespawnTime = TimeSpan.FromMilliseconds(lifetime.Value);
            remoteControlledCreature.SetGroup(bandwidthHandler);

            ClosestWalkablePositionFinder finder = new ClosestWalkablePositionFinder(Zone, targetPosition);
            Position position = finder.FindOrThrow();

            remoteControlledCreature.HomePosition = position;
            remoteControlledCreature.HomeRange = operationalRange.Value;
            remoteControlledCreature.Orientation = FastRandom.NextInt(0, 3) * 0.25;
            remoteControlledCreature.CallForHelp = true;

            BeamBuilder deployBeamBuilder = Beam.NewBuilder()
                .WithType(BeamType.dock_in)
                .WithSource(remoteControlledCreature.CommandRobot)
                .WithTarget(remoteControlledCreature)
                .WithState(BeamState.Hit)
                .WithDuration(TimeSpan.FromSeconds(5));

            remoteControlledCreature.AddToZone(Zone, position, ZoneEnterType.Default, deployBeamBuilder);
            ConsumeAmmo();
        }

        private Position GetSpawnPosition(Position spawnOrigin)
        {
            double spawnRangeMin = SpawnRangeMin;
            double spawnRangeMax = SpawnRangeMax;
            Position spawnPosition = spawnOrigin.GetRandomPositionInRange2D(spawnRangeMin, spawnRangeMax).Clamp(Zone.Size);

            return spawnPosition;
        }
    }
}
