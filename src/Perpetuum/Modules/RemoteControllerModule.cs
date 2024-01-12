using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Finders;
using Perpetuum.Zones.Finders.PositionFinders;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.NpcSystem.AI.Behaviors;
using Perpetuum.Zones.RemoteControl;
using System;
using System.Linq;

namespace Perpetuum.Modules
{
    public class RemoteControllerModule : ActiveModule
    {
        private const int SentryTurretHeight = 7;
        private const double TurretDeployRange = 2;
        private const double SpawnRangeMin = 2;
        private const double SpawnRangeMax = 5;
        private readonly ModuleProperty bandwidthMax;
        private BandwidthHandler bandwidthHandler;

        public double BandwidthMax
        {
            get { return bandwidthMax.Value; }
        }

        public RemoteControllerModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags, true)
        {
            optimalRange.AddEffectModifier(AggregateField.effect_ew_optimal_range_modifier);

            bandwidthMax = new ModuleProperty(this, AggregateField.remote_control_bandwidth_max);
            this.AddProperty(bandwidthMax);

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

            var ammo = GetAmmo() as RemoteControlledUnit;

            HasFreeBandwidthFor(ammo).ThrowIfFalse(ErrorCodes.MaxBandwidthExceed);

            Position targetPosition;

            if (ammo.ED.Options.TurretType != TurretType.CombatDrone)
            {

                var myLock = GetLock();

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

                Zone.Units
                .OfType<RemoteControlledCreature>()
                .WithinRange(Zone.FixZ(targetPosition), TurretDeployRange)
                .Any()
                .ThrowIfTrue(ErrorCodes.RemoteControlledTurretInRange);

                var r = Zone.IsInLineOfSight(ParentRobot, targetPosition.AddToZ(SentryTurretHeight), false);

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

            var player = this.ParentRobot is Player robotAsPlayer
                ? robotAsPlayer
                : null;


            if (player != null)
            {
                ammo.CheckEnablerExtensionsAndThrowIfFailed(player.Character, ErrorCodes.ExtensionLevelMismatchTerrain);
            }

            RemoteControlledCreature remoteControlledCreature = null;

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
                PerpetuumException.Create(ErrorCodes.InvalidAmmoDefinition);
            }

            if (player != null)
            {
                remoteControlledCreature.SetPlayer(player);
            }

            remoteControlledCreature.Owner = this.ParentRobot.Owner;
            remoteControlledCreature.SetBandwidthUsage(ammo.RemoteChannelBandwidthUsage);

            UseRemoteChannel(remoteControlledCreature);

            var despawnTimeMod = ammo.GetPropertyModifier(AggregateField.despawn_time);

            remoteControlledCreature.DespawnTime = TimeSpan.FromMilliseconds(despawnTimeMod.Value);
            remoteControlledCreature.SetGroup(bandwidthHandler);

            var finder = new ClosestWalkablePositionFinder(Zone, targetPosition);
            var position = finder.FindOrThrow();

            remoteControlledCreature.HomePosition = position;
            remoteControlledCreature.HomeRange = 50;
            remoteControlledCreature.Orientation = FastRandom.NextInt(0, 3) * 0.25;
            remoteControlledCreature.CallForHelp = true;

            var deployBeamBuilder = Beam.NewBuilder()
                .WithType(BeamType.dock_in)
                .WithSource(remoteControlledCreature.Player)
                .WithTarget(remoteControlledCreature)
                .WithState(BeamState.Hit)
                .WithDuration(TimeSpan.FromSeconds(5));

            remoteControlledCreature.AddToZone(Zone, position, ZoneEnterType.Default, deployBeamBuilder);
            Logger.Info($"[Remote Control] - spawned turret {remoteControlledCreature.Eid} of type {ammo.ED.Options.TurretType} owned by {remoteControlledCreature.Owner} represented by player {remoteControlledCreature.Player} at {targetPosition}");

            ConsumeAmmo();
        }

        private Position GetSpawnPosition(Position spawnOrigin)
        {
            var spawnRangeMin = SpawnRangeMin;
            var spawnRangeMax = SpawnRangeMax;
            var spawnPosition = spawnOrigin.GetRandomPositionInRange2D(spawnRangeMin, spawnRangeMax).Clamp(Zone.Size);

            return spawnPosition;
        }
    }
}
