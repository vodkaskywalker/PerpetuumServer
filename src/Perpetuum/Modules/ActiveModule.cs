using System;
using System.Collections.Generic;
using System.Diagnostics;
using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.Modules
{
    public abstract partial class ActiveModule : Module
    {
        private Lock _lock;
        protected readonly ModuleProperty coreUsage;
        protected readonly CycleTimeProperty cycleTime;
        protected readonly ItemProperty falloff = ItemProperty.None;
        protected readonly ModuleProperty optimalRange;

        private readonly CategoryFlags _ammoCategoryFlags;

        protected ActiveModule(CategoryFlags ammoCategoryFlags,bool ranged = false)
        {
            IsRanged = ranged;
            coreUsage = new ModuleProperty(this,AggregateField.core_usage);
            AddProperty(coreUsage);
            cycleTime = new CycleTimeProperty(this);
            AddProperty(cycleTime);

            if (ranged)
            {
                optimalRange = new OptimalRangeProperty(this);
                AddProperty(optimalRange);
                falloff = new FalloffProperty(this);
                AddProperty(falloff);
            }

            _ammoCategoryFlags = ammoCategoryFlags;
        }

        public override void Initialize()
        {
            InitState();
            InitAmmo();
            base.Initialize();
        }


        protected ActiveModule(bool ranged) : this(CategoryFlags.undefined,ranged)
        {
        }

        public TimeSpan CycleTime => TimeSpan.FromMilliseconds(cycleTime.Value);

        public double CoreUsage => coreUsage.Value;

        public double OptimalRange => optimalRange.Value;

        public double Falloff => falloff.Value;

        public Lock Lock
        {
            [CanBeNull] private get { return _lock; }
            set
            {
                if (_lock != null)
                    _lock.Changed -= LockChangedHandler;

                _lock = value;

                if (_lock != null)
                    _lock.Changed += LockChangedHandler;
            }
        }

        public bool IsRanged { get; private set; }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        private void LockChangedHandler(Lock @lock)
        {
            if (State.Type == ModuleStateType.Idle || State.Type == ModuleStateType.AmmoLoad)
                return;

            var shutdown = @lock.State == LockState.Disabled || (ED.AttributeFlags.PrimaryLockedTarget && !@lock.Primary);
            if (!shutdown) 
                return;

            State.SwitchTo(ModuleStateType.Shutdown);
            _lock = null;
        }

        public bool IsInRange(Position position)
        {
            Debug.Assert(ParentRobot != null, "ParentRobot != null");
            return !IsRanged || ParentRobot.IsInRangeOf3D(position,OptimalRange + Falloff);
        }

        public void ForceUpdate()
        {
            SendModuleStateToPlayer();
            SendAmmoUpdatePacketToPlayer();
        }

        private BeamType GetBeamType()
        {
            var ammo = GetAmmo();
            return ammo != null ? BeamHelper.GetBeamByDefinition(ammo.Definition) : BeamHelper.GetBeamByDefinition(Definition);
        }

        private void SendModuleStateToPlayer()
        {
            var state = State;

            var player = ParentRobot as Player;
            if (player == null)
                return;

            var packet = new Packet(ZoneCommand.ModuleChangeState);
            Debug.Assert(ParentComponent != null, "ParentComponent != null");
            packet.AppendByte((byte)ParentComponent.Type);
            packet.AppendByte((byte) Slot);
            packet.AppendByte((byte)state.Type);

            var timed = state as ITimedModuleState;
            if (timed == null)
            {
                packet.AppendInt(0);
                packet.AppendInt(0);
            }
            else
            {
                packet.AppendInt((int)timed.Timer.Interval.TotalMilliseconds);
                packet.AppendInt((int)timed.Timer.Elapsed.TotalMilliseconds);
            }

            player.Session.SendPacket(packet);
        }

        public void Update(TimeSpan time)
        {
            _states.Update(time);
        }

        protected abstract void OnAction();

        protected virtual void HandleOffensivePVPCheck(Player parentPlayer, UnitLock unitLockTarget)
        {
            if (parentPlayer != null)
            {
                // pvp ellenorzes
                (unitLockTarget.Target as Player)?.CheckPvp().ThrowIfError();
            }
        }

        protected Lock GetLock()
        {
            var currentLock = Lock.ThrowIfNull(ErrorCodes.LockTargetNotFound);
            currentLock.State.ThrowIfNotEqual(LockState.Locked, ErrorCodes.LockIsInProgress);

            var unitLockTarget = currentLock as UnitLock;
            if (unitLockTarget != null)
            {
                IsInRange(unitLockTarget.Target.CurrentPosition).ThrowIfFalse(ErrorCodes.TargetOutOfRange);

                var parentPlayer = ParentRobot as Player;

                if (ED.AttributeFlags.OffensiveModule)
                {
                    HandleOffensivePVPCheck(parentPlayer, unitLockTarget);
                    Debug.Assert(ParentRobot != null, "ParentRobot != null");
                    ParentRobot.OnAggression(unitLockTarget.Target);
                }
                else if ((parentPlayer != null) && (unitLockTarget.Target is Player) && ED.AttributeFlags.PvpSupport)
                {
                    parentPlayer.OnPvpSupport(unitLockTarget.Target);
                }
            }
            else
            {
                var terrainLockTarget = currentLock as TerrainLock;
                if (terrainLockTarget != null)
                {
                    IsInRange(terrainLockTarget.Location).ThrowIfFalse(ErrorCodes.TargetOutOfRange);
                }
            }

            return currentLock;
        }

        public override void UpdateAllProperties()
        {
            base.UpdateAllProperties();

            var ammo = GetAmmo();
            ammo?.UpdateAllProperties();
        }

        protected void CreateBeam(Unit target, BeamState beamState)
        {
            CreateBeam(target, beamState, 0, 0, 0);
        }

        protected int CreateBeam(Unit target, BeamState beamState, int duration, double bulletTime)
        {
            return CreateBeam(target, beamState, duration, bulletTime, 0);
        }

        private int CreateBeam(Unit target, BeamState beamState, int duration, double bulletTime,int visibility)
        {
            var delay = 0;
            var beamType = GetBeamType();

            if (beamType <= 0)
                return delay;

            delay = BeamHelper.GetBeamDelay(beamType);

            if (duration == 0)
                duration = (int)CycleTime.TotalMilliseconds;

            Debug.Assert(ParentComponent != null, "ParentComponent != null");
            var slot = ParentComponent.Type == RobotComponentType.Chassis ? Slot : 0xff; // -1

            var builder = Beam.NewBuilder().WithType(beamType)
                .WithSlot(slot)
                .WithSource(ParentRobot)
                .WithState(beamState)
                .WithBulletTime(bulletTime)
                .WithDuration(duration)
                .WithTarget(target)
                .WithVisibility(visibility);

            Zone.CreateBeam(builder);
            return delay;
        }

        protected void CreateBeam(Position location, BeamState beamState)
        {
            CreateBeam(location, beamState, 0, 0, 0);
        }

        protected int CreateBeam(Position location, BeamState beamState, int duration, double bulletTime)
        {
            return CreateBeam(location, beamState, duration, bulletTime,0);
        }

        private int CreateBeam(Position location, BeamState beamState, int duration, double bulletTime,int visibility)
        {
            var delay = 0;
            var beamType = GetBeamType();

            if (beamType <= 0) 
                return delay;

            delay = BeamHelper.GetBeamDelay(beamType);

            if (duration == 0)
                duration = (int) CycleTime.TotalMilliseconds;

            Debug.Assert(ParentComponent != null, "ParentComponent != null");
            var slot = ParentComponent.Type == RobotComponentType.Chassis ? Slot : 0xff; // -1

            var builder = Beam.NewBuilder().WithType(beamType)
                .WithSlot(slot)
                .WithSource(ParentRobot)
                .WithState(beamState)
                .WithBulletTime(bulletTime)
                .WithDuration(duration)
                .WithTargetPosition(location)
                .WithVisibility(visibility);

            Zone.CreateBeam(builder);
            return delay;
        }

        protected override void OnUpdateProperty(AggregateField field)
        {
            switch (field)
            {
                case AggregateField.core_usage:
                case AggregateField.effect_core_usage_gathering_modifier:
                {
                    coreUsage.Update();
                    break;
                }
                case AggregateField.cycle_time:
                case AggregateField.effect_weapon_cycle_time_modifier:
                case AggregateField.effect_gathering_cycle_time_modifier:
                {
                    cycleTime.Update();
                    break;
                }
                case AggregateField.optimal_range:
                case AggregateField.effect_optimal_range_modifier:
                case AggregateField.effect_ew_optimal_range_modifier:
                case AggregateField.module_missile_range_modifier:
                case AggregateField.effect_missile_range_modifier:
                {
                    optimalRange.Update();
                    break;
                }
                case AggregateField.falloff:
                {
                    falloff.Update();
                    break;
                }
            }

            base.OnUpdateProperty(field);
        }

        public override void Unequip(Container container)
        {
            UnequipAmmoToContainer(container);
            base.Unequip(container);
        }

        protected double ModifyValueByOptimalRange(Unit target,double value)
        {
            Debug.Assert(ParentRobot != null, "ParentRobot != null");

            var distance = ParentRobot.GetDistance(target);
            if (distance <= OptimalRange)
                return value;

            if (distance > OptimalRange + Falloff)
                return 0.0;

            var x = (distance - OptimalRange) / Falloff;
            var m = Math.Cos(x * Math.PI) / 2 + 0.5;
            return value * m;
        }

        protected LOSResult GetLineOfSight(Unit target)
        {
            Debug.Assert(ParentRobot != null, "ParentRobot != null");
            var visibility = ParentRobot.GetVisibility(target);
            return visibility?.GetLineOfSight(IsCategory(CategoryFlags.cf_missiles)) ?? LOSResult.None;
        }

        protected LOSResult GetLineOfSight(Position location)
        {
            Debug.Assert(ParentRobot != null, "ParentRobot != null");
            var losResult = ParentRobot.Zone.IsInLineOfSight(ParentRobot,location,IsCategory(CategoryFlags.cf_missiles));
            return losResult;
        }

        protected bool LOSCheckAndCreateBeam(Unit target)
        {
            var result = GetLineOfSight(target);
            if (result.hit)
            {
                var beamState = (result.blockingFlags != BlockingFlags.Undefined) ? BeamState.AlignToTerrain : BeamState.Hit;
                CreateBeam(result.position, beamState);
                return false;
            }

            CreateBeam(target, BeamState.Hit);
            return true;
        }

        protected void OnError(ErrorCodes error)
        {
            SendModuleErrorToPlayer(error);
        }

        private void SendModuleErrorToPlayer(ErrorCodes error)
        {
            var player = ParentRobot as Player;
            if (player == null)
                return;

            var packet = new CombatLogPacket(error, this, _lock);
            player.Session.SendPacket(packet);
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var result = base.ToDictionary();

            result.Add(k.ammoCategoryFlags, (long)_ammoCategoryFlags);

            var ammo = GetAmmo();
            if (ammo == null)
                return result;

            result.Add(k.ammo, ammo.ToDictionary());
            result.Add(k.ammoQuantity, ammo.Quantity);

            return result;
        }

        public override double Volume
        {
            get
            {
                var volume = base.Volume;

                var ammo = GetAmmo();
                if (ammo != null)
                    volume += ammo.Volume;

                return volume;
            }
        }

        public override void UpdateRelatedProperties(AggregateField field)
        {
            var ammo = GetAmmo();
            ammo?.UpdateRelatedProperties(field);

            base.UpdateRelatedProperties(field);
        }
    }
}