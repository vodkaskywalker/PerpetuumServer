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
using Perpetuum.Zones.NpcSystem;
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

        /*
        [CanBeNull]
        public RemoteChannel GetRemoteChannel(long channelId)
        {
            //return bandwidthHandler.GetRemoteChannel(channelId);
        }
        */

        /*
        [CanBeNull]
        public RemoteChannel GetRemoteChannelByUnit(Unit unit)
        {
            //return bandwidthHandler.GetRemoteChannelByUnit(unit);
        }
        */

        public bool HasFreeBandwidthFor(RemoteControlledUnit unit)
        {
            return bandwidthHandler.HasFreeBandwidthFor(unit);
        }

        public void UseRemoteChannel(RemoteControlledTurret turret)
        {
            bandwidthHandler.UseRemoteChannel(turret);
            turret.RemoteChannelDeactivated += bandwidthHandler.OnRemoteChannelDeactivated;
            bandwidthHandler.Update();
        }

        /*
        public void UseRemoteChannel(RemoteChannel newChannel)
        {
            bandwidthHandler.UseRemoteChannel(newChannel);
        }
        */

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

            Position? lockPosition;

            var myLock = GetLock();

            if (myLock is TerrainLock)
            {
                lockPosition = (myLock as TerrainLock).Location;
            }
            else if (myLock is UnitLock)
            {
                lockPosition = (myLock as UnitLock).Target.CurrentPosition;
            }
            else
            {
                OnError(ErrorCodes.InvalidLockType);

                return;
            }

            Position targetPosition = lockPosition.Value;
            
            Zone.Units
                .OfType<RemoteControlledTurret>()
                .WithinRange(Zone.FixZ(targetPosition), TurretDeployRange)
                .Any()
                .ThrowIfTrue(ErrorCodes.RemoteControlledTurretInRange);
            
            var r = Zone.IsInLineOfSight(ParentRobot, targetPosition.AddToZ(SentryTurretHeight), false);

            if (r.hit)
            {
                OnError(ErrorCodes.LOSFailed);

                return;
            }

            var ammo = GetAmmo() as RemoteControlledUnit;

            HasFreeBandwidthFor(ammo).ThrowIfFalse(ErrorCodes.MaxBandwidthExceed);

            var player = this.ParentRobot is Player robotAsPlayer
                ? robotAsPlayer
                : null;


            if (player != null)
            {
                ammo.CheckEnablerExtensionsAndThrowIfFailed(player.Character, ErrorCodes.ExtensionLevelMismatchTerrain);
            }

            RemoteControlledTurret fieldTurret = null;

            if (ammo.ED.Options.TurretType == TurretType.Sentry)
            {
                fieldTurret = (SentryTurret)Factory.CreateWithRandomEID(ammo.ED.Options.TurretId);
            }
            else if (ammo.ED.Options.TurretType == TurretType.Mining || ammo.ED.Options.TurretType == TurretType.Harvesting)
            {
                fieldTurret = (IndustrialTurret)Factory.CreateWithRandomEID(ammo.ED.Options.TurretId);
                (fieldTurret as IndustrialTurret).SetTurretType(ammo.ED.Options.TurretType);
            }
            else
            {
                PerpetuumException.Create(ErrorCodes.InvalidAmmoDefinition);
            }

            if (player != null)
            {
                fieldTurret.SetPlayer(player);
            }

            fieldTurret.Owner = this.ParentRobot.Owner;
            fieldTurret.Behavior = Behavior.Create(BehaviorType.RemoteControlled);
            fieldTurret.SetBandwidthUsage(ammo.RemoteChannelBandwidthUsage);

            UseRemoteChannel(fieldTurret);

            var despawnTimeMod = ammo.GetPropertyModifier(AggregateField.despawn_time);

            fieldTurret.DespawnTime = TimeSpan.FromMilliseconds(despawnTimeMod.Value);
            fieldTurret.SetGroup(bandwidthHandler);

            var finder = new ClosestWalkablePositionFinder(Zone, targetPosition);
            var position = finder.FindOrThrow();

            fieldTurret.HomePosition = position;
            fieldTurret.HomeRange = 30;
            fieldTurret.Orientation = FastRandom.NextInt(0, 3) * 0.25;
            fieldTurret.CallForHelp = true;

            var deployBeamBuilder = Beam.NewBuilder()
                .WithType(BeamType.dock_in)
                .WithSource(fieldTurret.Player)
                .WithTarget(fieldTurret)
                .WithState(BeamState.Hit)
                .WithDuration(TimeSpan.FromSeconds(5));

            fieldTurret.AddToZone(Zone, position, ZoneEnterType.Default, deployBeamBuilder);
            Logger.Info($"[Remote Control] - spawned turret {fieldTurret.Eid} of type {ammo.ED.Options.TurretType} owned by {fieldTurret.Owner} represented by player {fieldTurret.Player} at {targetPosition}");

            ConsumeAmmo();
        }
    }
}
