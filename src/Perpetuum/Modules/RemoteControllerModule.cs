using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.Finders;
using Perpetuum.Zones.Finders.PositionFinders;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.RemoteControl;
using System;
using System.Diagnostics;
using System.Linq;

namespace Perpetuum.Modules
{
    public class RemoteControllerModule : ActiveModule
    {
        private const int SentryTurretHeight = 7;
        private const double SentryTurretDeployRange = 2;
        private readonly ModuleProperty bandwidthMax;
        private BandwidthHandler bandwidthHandler;
        protected readonly EffectToken effectToken = EffectToken.NewToken();

        public double BandwidthMax
        {
            get { return bandwidthMax.Value; }
        }

        public RemoteControllerModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags, true)
        {
            optimalRange.AddEffectModifier(AggregateField.effect_ew_optimal_range_modifier);

            bandwidthMax = new ModuleProperty(this, AggregateField.remote_control_bandwidth_max);
            this.AddProperty(bandwidthMax);

            var remoteControlDamageModifier = new ModuleProperty(this, AggregateField.remote_control_damage_modifier);
            this.AddProperty(remoteControlDamageModifier);

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

        [CanBeNull]
        public RemoteChannel GetRemoteChannel(long channelId)
        {
            return bandwidthHandler.GetRemoteChannel(channelId);
        }

        [CanBeNull]
        public RemoteChannel GetRemoteChannelByUnit(Unit unit)
        {
            return bandwidthHandler.GetRemoteChannelByUnit(unit);
        }

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

        public void UseRemoteChannel(RemoteChannel newChannel)
        {
            bandwidthHandler.UseRemoteChannel(newChannel);
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
            zone.Units
                .OfType<SentryTurret>()
                .WithinRange(targetPosition, SentryTurretDeployRange)
                .Any()
                .ThrowIfTrue(ErrorCodes.BlobEmitterInRange);

            var r = zone.IsInLineOfSight(ParentRobot, targetPosition, false);

            if (r.hit)
            {
                OnError(ErrorCodes.LOSFailed);

                return;
            }

            var ammo = GetAmmo() as RemoteControlledUnit;

            if (this.ParentRobot is Player player)
            {
                ammo.CheckEnablerExtensionsAndThrowIfFailed(player.Character, ErrorCodes.ExtensionLevelMismatchTerrain);
            }

            RemoteControlledTurret fieldTurret = null;

            if (ammo.ED.Options.TurretType == TurretType.Senrty)
            {
                fieldTurret = (SentryTurret)Factory.CreateWithRandomEID(ammo.ED.Options.TurretId);
            }
            else if (ammo.ED.Options.TurretType == TurretType.Mining)
            {
                fieldTurret = (MiningTurret)Factory.CreateWithRandomEID(ammo.ED.Options.TurretId);
            }

            fieldTurret.Owner = this.ParentRobot.Eid;

            HasFreeBandwidthFor(ammo).ThrowIfFalse(ErrorCodes.MaxBandwidthExceed);
            UseRemoteChannel(fieldTurret);

            var despawnTimeMod = ammo.GetPropertyModifier(AggregateField.despawn_time);

            var despawnTime = TimeSpan.FromMilliseconds(despawnTimeMod.Value);

            fieldTurret.DespawnTime = despawnTime;

            fieldTurret.SetGroup(bandwidthHandler);

            var finder = new ClosestWalkablePositionFinder(zone, targetPosition);
            var position = finder.FindOrThrow();
            var beamBuilder = Beam.NewBuilder()
                .WithType(BeamType.deploy_device_01)
                .WithPosition(targetPosition)
                .WithDuration(TimeSpan.FromSeconds(5));

            fieldTurret.AddToZone(zone, position, ZoneEnterType.Default, beamBuilder);

            var effectBuilder = this.ParentRobot.NewEffectBuilder();

            SetupEffect(effectBuilder);

            effectBuilder
                .WithToken(effectToken)
                .WithDuration(despawnTime);

            this.ParentRobot.ApplyEffect(effectBuilder);

            ConsumeAmmo();
        }

        protected void SetupEffect(EffectBuilder effectBuilder)
        {
            effectBuilder.SetType(EffectType.effect_remote_control);
        }
    }
}
