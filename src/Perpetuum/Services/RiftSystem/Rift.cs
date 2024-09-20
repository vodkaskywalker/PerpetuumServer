using Perpetuum.Deployers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Blobs.BlobEmitters;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.Teleporting.Strategies;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Perpetuum.Services.RiftSystem
{
    public interface IConfigurablePortal
    {
        void SetConfig(CustomRiftConfig riftConfig);
        void SetTarget(IZone zone, Position position);
    }

    public interface IDespawningPortal
    {
        void SetDespawnTime(TimeSpan despawnTime);
    }

    /// <summary>
    /// Super class to all Rifts
    /// </summary>
    public abstract class Portal : Unit, IUsableItem
    {
        public virtual void UseItem(Player player)
        {
            player.HasTeleportSicknessEffect.ThrowIfTrue(ErrorCodes.TeleportTimerStillRunning);
            player.HasNoxTeleportNegationEffect.ThrowIfTrue(ErrorCodes.NoxTeleportForbidden);
            player.HasPvpEffect.ThrowIfTrue(ErrorCodes.CantBeUsedInPvp);
            player.CurrentPosition.IsInRangeOf3D(CurrentPosition, 8).ThrowIfFalse(ErrorCodes.TeleportOutOfRange);
        }

        protected override bool IsHostileFor(Unit unit)
        {
            return unit.IsHostile(this);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }
    }

    /// <summary>
    /// Rifts that despawn
    /// </summary>
    public abstract class DespawningPortal : Portal, IDespawningPortal
    {
        private UnitDespawnHelper _despawnHelper;

        public void SetDespawnTime(TimeSpan despawnTime)
        {
            _despawnHelper = UnitDespawnHelper.Create(this, despawnTime);
        }

        protected override void OnUpdate(TimeSpan time)
        {
            _despawnHelper?.Update(time, this);
            base.OnUpdate(time);
        }
    }

    /// <summary>
    /// Rifts that can have a configurable destination when used
    /// </summary>
    public class DespawningTargettedPortal : DespawningPortal, IConfigurablePortal
    {
        protected readonly ITeleportStrategyFactories _teleportStrategyFactories;
        public IZone TargetZone { get; private set; }
        public Position TargetPosition { get; private set; }
        public CustomRiftConfig RiftConfig { get; private set; }
        private int uses = 0;

        public DespawningTargettedPortal(ITeleportStrategyFactories teleportStrategyFactories)
        {
            _teleportStrategyFactories = teleportStrategyFactories;
        }

        public void SetConfig(CustomRiftConfig riftConfig)
        {
            RiftConfig = riftConfig;
            // Only set despawner if config is despawning
            if (RiftConfig.IsDespawning)
            {
                SetDespawnTime(RiftConfig.Lifespan);
            }
        }

        public void SetTarget(IZone zone, Position position)
        {
            TargetZone = zone;
            TargetPosition = position;
        }

        private bool HasTarget => TargetZone != null && !TargetPosition.Equals(Position.Empty);

        private bool IsUsageExceeded()
        {
            return !RiftConfig.InfiniteUses && RiftConfig.MaxUses < uses;
        }

        private void IncrementUsage()
        {
            if (!RiftConfig.InfiniteUses)
            {
                uses++;
            }
        }

        private bool IsExcluded(Player player)
        {
            return RiftConfig.IsExcluded(player.ED.CategoryFlags);
        }

        public override void UseItem(Player player)
        {
            HasTarget.ThrowIfFalse(ErrorCodes.NoTeleportDestinations);
            base.UseItem(player);

            IsUsageExceeded().ThrowIfTrue(ErrorCodes.MaximumAllowedRegistrationExceeded);

            IsExcluded(player).ThrowIfTrue(ErrorCodes.RobotWrongType);

            TeleportToAnotherZone teleport = _teleportStrategyFactories.TeleportToAnotherZoneFactory(TargetZone);
            teleport.TargetPosition = TargetPosition;
            _ = teleport.DoTeleportAsync(player);

            IncrementUsage();

            if (IsUsageExceeded())
            {
                Kill();
            }
        }
    }

    public class StrongholdExitRift : DespawningTargettedPortal
    {
        public StrongholdExitRift(ITeleportStrategyFactories teleportStrategyFactories) : base(teleportStrategyFactories) { }
    }

    public class StrongholdEntryRift : DespawningTargettedPortal
    {
        public StrongholdEntryRift(ITeleportStrategyFactories teleportStrategyFactories) : base(teleportStrategyFactories) { }
    }

    /// <summary>
    /// RandomRift (used as the Rift Echo after a TAP)
    /// </summary>
    public class RandomRiftPortal : DespawningPortal
    {
        private readonly ITeleportStrategyFactories _teleportStrategyFactories;
        private Position _targetPosition;

        public RandomRiftPortal(ITeleportStrategyFactories teleportStrategyFactories)
        {
            _teleportStrategyFactories = teleportStrategyFactories;
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            Rift rift = Zone.Units.OfType<Rift>().RandomElement();
            if (rift != null)
            {
                _targetPosition = rift.CurrentPosition;
            }

            base.OnEnterZone(zone, enterType);
        }

        public override void UseItem(Player player)
        {
            base.UseItem(player);

            TeleportWithinZone teleport = _teleportStrategyFactories.TeleportWithinZoneFactory();
            teleport.TargetPosition = _targetPosition;
            teleport.ApplyTeleportSickness = true;
            teleport.ApplyInvulnerable = true;
            _ = teleport.DoTeleportAsync(player);
        }
    }

    public class RiftNpcGroupInfo
    {
        public int presenceID;
        public TimeSpan presenceLifeTime;
        public int wavesCount;
        public Player ownerPlayer;
    }

    /// <summary>
    /// TAP deployment
    /// </summary>
    public class RiftActivator : ItemDeployerBase
    {
        public override void Deploy(IZone zone, Player player)
        {
            Rift rift = player.GetUnitsWithinRange<Rift>(5).FirstOrDefault();
            if (rift == null)
            {
                throw new PerpetuumException(ErrorCodes.RiftOutOfRange);
            }

            if (ED.Tier.level > rift.MaxTAPLevel)
            {
                throw new PerpetuumException(ErrorCodes.RiftLevelMismatch);
            }

            if (zone is StrongHoldZone)
            {
                throw new PerpetuumException(ErrorCodes.ItemNotUsable);
            }

            RiftNpcGroupInfo info = new RiftNpcGroupInfo();
            Debug.Assert(DeployableItemEntityDefault.Config.npcPresenceId != null, "DeployableItemEntityDefault.Config.npcPresenceId != null");
            info.presenceID = (int)DeployableItemEntityDefault.Config.npcPresenceId;
            Debug.Assert(DeployableItemEntityDefault.Config.lifeTime != null, "DeployableItemEntityDefault.Config.lifeTime != null");
            info.presenceLifeTime = TimeSpan.FromMilliseconds((double)DeployableItemEntityDefault.Config.lifeTime);
            Debug.Assert(DeployableItemEntityDefault.Config.waves != null, "DeployableItemEntityDefault.Config.waves != null");
            info.wavesCount = (int)DeployableItemEntityDefault.Config.waves;
            info.ownerPlayer = player;

            if (!rift.TryActivate(info))
            {
                throw new PerpetuumException(ErrorCodes.WTFErrorMedicalAttentionSuggested);
            }

            LogTransaction(player, this);

            SummonEggEventInfo seei = new SummonEggEventInfo(player, DeployableItemEntityDefault.Definition, player.CurrentPosition);
            player.MissionHandler.EnqueueMissionEventInfo(seei);
        }
    }

    /// <summary>
    /// Regular Rift, despawns, can activate a TAP on it, can jump, emits interference
    /// </summary>
    public class Rift : DespawningPortal, IBlobEmitter
    {
        private readonly ITeleportStrategyFactories _teleportStrategyFactories;
        private readonly BlobEmitter _blobEmitter;

        public int MaxTAPLevel { get; private set; }

        public Rift(ITeleportStrategyFactories teleportStrategyFactories)
        {
            _teleportStrategyFactories = teleportStrategyFactories;
            _blobEmitter = new BlobEmitter(this);
        }

        public void SetLevel(int maxLevel)
        {
            MaxTAPLevel = maxLevel;
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
        }

        private int _activated;

        public bool TryActivate(RiftNpcGroupInfo info)
        {
            IZone zone = Zone;
            if (zone == null)
            {
                return false;
            }

            if (zone is StrongHoldZone)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref _activated, 1, 0) == 1)
            {
                return false;
            }

            zone.CreateBeam(BeamType.npc_egg_beam, builder => builder.WithPosition(CurrentPosition).WithDuration(6000));

            _ = Task.Delay(6000).ContinueWith(t =>
            {
                DynamicPoolPresence presence = (DynamicPoolPresence)zone.PresenceManager.CreatePresence(info.presenceID);
                presence.DynamicPosition = CurrentPosition;
                presence.LifeTime = info.presenceLifeTime;
                presence.Summoner = info.ownerPlayer.Character;
                presence.Init(info.wavesCount);

                zone.PresenceManager.AddPresence(presence);
                RemoveFromZone();
            });

            Logger.Info("Rift activated.");
            return true;
        }

        public override void UseItem(Player player)
        {
            base.UseItem(player);

            Rift nearestRift = Zone.Units.OfType<Rift>().Where(rift => rift != this).GetNearestUnit(CurrentPosition);
            if (nearestRift == null)
            {
                throw new PerpetuumException(ErrorCodes.WTFErrorMedicalAttentionSuggested);
            }

            TeleportWithinZone teleport = _teleportStrategyFactories.TeleportWithinZoneFactory();
            teleport.TargetPosition = nearestRift.CurrentPosition;
            teleport.ApplyTeleportSickness = true;
            teleport.ApplyInvulnerable = true;
            _ = teleport.DoTeleportAsync(player);

        }

        public double BlobEmission => _blobEmitter.BlobEmission;

        public double BlobEmissionRadius => _blobEmitter.BlobEmissionRadius;
    }
}
