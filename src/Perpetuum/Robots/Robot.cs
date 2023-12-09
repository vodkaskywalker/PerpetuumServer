using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Builders;
using Perpetuum.Common;
using Perpetuum.Containers;
using Perpetuum.Containers.SystemContainers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Items.Templates;
using Perpetuum.Modules;
using Perpetuum.Services.ExtensionService;
using Perpetuum.Services.Insurance;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.DamageProcessors;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;

namespace Perpetuum.Robots
{
    public abstract partial class Robot : Unit
    {
        private Lazy<IEnumerable<Module>> _modules;
        private Lazy<IEnumerable<ActiveModule>> _activeModules;
        private Lazy<IEnumerable<Item>> _components;
        private Lazy<IEnumerable<RobotComponent>> _robotComponents;

        public RobotHelper RobotHelper { protected get; set; }
        public InsuranceHelper InsuranceHelper { protected get; set; }

        public RobotTemplate Template { get; set; }

        public bool IsSelected => RobotHelper.IsSelected(this);

        public override double Health
        {
            get
            {
                if (IsRepackaged)
                {
                    return base.Health;
                }

                return ArmorMax > 0.0 && Armor > 0.0
                    ? Armor.Ratio(ArmorMax) * 100
                    : base.Health;
            }
        }

        public override double Mass
        {
            get { return RobotComponents.Sum(c => c.Mass); }
        }

        public bool HasModule
        {
            get { return Modules.Any(); }
        }

        public bool IsItemsInContainer
        {
            get
            {
                if (IsRepackaged)
                {
                    return false;
                }

                var robotInventory = GetContainer();

                if (robotInventory == null)
                {
                    return false;
                }

                return robotInventory.HasChildren;
            }
        }

        public override bool IsStackable
        {
            get { return base.IsStackable && IsRepackaged; }
        }

        public IEnumerable<Extension> ExtensionBonusEnablerExtensions
        {
            get { return ExtensionBonuses.Select(cb => new Extension(cb.extensionId, 1)); }
        }

        protected IEnumerable<ExtensionBonus> ExtensionBonuses
        {
            get { return RobotComponents.SelectMany(component => component.ExtensionBonuses); }
        }

        public IEnumerable<Module> Modules
        {
            get { return _modules.Value; }
        }

        public IEnumerable<ActiveModule> ActiveModules
        {
            get { return _activeModules.Value; }
        }

        public IEnumerable<Item> Components
        {
            get { return _components.Value; }
        }

        public IEnumerable<RobotComponent> RobotComponents
        {
            get { return _robotComponents.Value; }
        }

        public override double Volume
        {
            get
            {
                if (IsRepackaged)
                {
                    return base.Volume;
                }

                var volume = RobotComponents.Sum(c => c.Volume);

                volume *= Quantity;

                return volume;
            }
        }

        public bool IsTrashed
        {
            get { return Trashcan.IsItemTrashed(this); }
        }

        protected Robot()
        {
            InitLockHander();
            InitProperties();
        }

        public override void Initialize()
        {
            InitComponents();
            base.Initialize();
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public void VisitModules(IEntityVisitor visitor)
        {
            foreach (var module in Modules)
            {
                module.AcceptVisitor(visitor);
            }
        }

        public void VisitRobotComponents(IEntityVisitor visitor)
        {
            foreach (var component in RobotComponents)
            {
                component.AcceptVisitor(visitor);
            }
        }

        public void VisitRobotInventory(IEntityVisitor visitor)
        {
            var container = GetContainer();

            container?.AcceptVisitor(visitor);
        }

        public override IDictionary<string, object> GetDebugInfo()
        {
            var info = base.GetDebugInfo();

            info.Add("locksCount", _lockHandler.Count);

            return info;
        }

        public void FullArmorRepair()
        {
            DynamicProperties.Update(k.armor, 1.0);
            Armor = ArmorMax;
        }

        public void FullCoreRecharge()
        {
            var currentCore = DynamicProperties.GetOrAdd<double>(k.currentCore);
            var coreMaxValue = CoreMax;

            if (currentCore >= coreMaxValue)
            {
                return;
            }

            DynamicProperties.Update(k.currentCore, coreMaxValue);
            Core = CoreMax;
        }

        [CanBeNull]
        public RobotInventory GetContainer()
        {
            return Children.OfType<RobotInventory>().FirstOrDefault();
        }

        public void StopAllModules()
        {
            foreach (var module in ActiveModules)
            {
                module.State.SwitchTo(ModuleStateType.Idle);
            }
        }

        public override void OnDeleteFromDb()
        {
            InsuranceHelper.DeleteAndInform(this);
            base.OnDeleteFromDb();
        }

        public void EmptyRobot(Character character, Container targetContainer, bool withContainer = true)
        {
            foreach (var module in Modules)
            {
                module.Unequip(targetContainer);
            }

            if (!withContainer)
            {
                return;
            }

            var container = GetContainer();

            container?.RelocateItems(character, character, container.GetItems(), targetContainer);
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var dictionary = base.ToDictionary();

            foreach (var component in RobotComponents)
            {
                dictionary.Add(component.ComponentName, component.ToDictionary());
            }

            var container = GetContainer();

            if (container != null)
            {
                dictionary.Add(k.container, container.ToDictionary());
            }

            dictionary.Add(k.decay, Decay);
            dictionary.Add(k.tint, Tint);

            return dictionary;
        }

        [CanBeNull]
        public Module FindModuleByCategoryFlag(CategoryFlags cf)
        {
            return Modules.FirstOrDefault(m => m.IsCategory(cf));
        }

        [CanBeNull]
        public Module GetModule(long moduleEid)
        {
            return Modules.FirstOrDefault(m => m.Eid == moduleEid);
        }

        [CanBeNull]
        public RobotComponent GetRobotComponent(string componentName)
        {
            return GetRobotComponent(componentName.ToEnum<RobotComponentType>());
        }

        [NotNull]
        public RobotComponent GetRobotComponentOrThrow(RobotComponentType componentType)
        {
            return GetRobotComponent(componentType).ThrowIfNull(ErrorCodes.RequiredComponentNotFound);
        }

        [CanBeNull]
        public RobotComponent GetRobotComponent(RobotComponentType componentType)
        {
            return RobotComponents.FirstOrDefault(c => c.Type == componentType);
        }

        public void CheckEnergySystemAndThrowIfFailed(Module module, bool isRemoving = false)
        {
            if (!CheckPowerGridForModule(module, isRemoving))
            {
                throw PerpetuumException.Create(ErrorCodes.OutOfPowergrid);
            }

            if (!CheckCpuForModule(module, isRemoving))
            {
                throw PerpetuumException.Create(ErrorCodes.OutOfCpu);
            }
        }

        public void CheckEnergySystemAndThrowIfFailed()
        {
            if (PowerGrid < 0)
            {
                throw PerpetuumException.Create(ErrorCodes.OutOfPowergrid).SetData("powerGrid", Math.Abs(PowerGridMax - PowerGrid)).SetData("powerGridMax", PowerGridMax);
            }

            if (Cpu < 0)
            {
                throw PerpetuumException.Create(ErrorCodes.OutOfCpu).SetData("cpu", Math.Abs(CpuMax - Cpu)).SetData("cpuMax", CpuMax);
            }
        }

        public void CreateComponents()
        {
            foreach (var component in Template.BuildComponents())
            {
                AddChild(component);
            }
        }

        [CanBeNull]
        public T GetRobotComponent<T>() where T : RobotComponent
        {
            return RobotComponents.OfType<T>().FirstOrDefault();
        }

        protected virtual void OnLockError(Lock @lock, ErrorCodes error)
        {
        }

        protected virtual void OnLockStateChanged(Lock @lock)
        {
            States.LockSomething = _lockHandler.Count > 0;

            var unitLock = @lock as UnitLock;

            if (unitLock != null)
            {
                UpdateTypes |= UnitUpdateTypes.Lock;
                UpdateVisibilityOf(unitLock.Target);
            }

            var builder = new AnonymousBuilder<Packet>(() => LockPacketBuilder.BuildPacket(@lock));

            OnBroadcastPacket(builder.ToProxy());
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);

            _lockHandler.Update(time);

            foreach (var robotComponent in RobotComponents)
            {
                robotComponent.Update(time);
            }
        }

        protected internal override double ComputeHeight()
        {
            return RobotComponents.Sum(c => c.ComputeHeight());
        }

        protected override void OnDamageTaken(Unit source, DamageTakenEventArgs args)
        {
            base.OnDamageTaken(source, args);

            var decayChance = this.decayChance.Value;
            var random = FastRandom.NextDouble();

            if (decayChance >= random)
            {
                var d = Decay;

                if (d > 0)
                {
                    d--;
                    Decay = d;
                }
            }

            this.Core -= args.TotalEnergyDamage;

            if (args.TotalSpeedDamage > 0)
            {
                var effectProperty = ItemPropertyModifier.Create(AggregateField.effect_massivness_speed_max_modifier, args.TotalSpeedDamage);
                effectProperty.Add(this.Massiveness);

                if (effectProperty.Value >= 1.0)
                {
                    effectProperty.ResetToDefaultValue();
                }

                var effectBuilder = this.NewEffectBuilder();
                var _token = EffectToken.NewToken();

                effectBuilder
                    .WithToken(_token)
                    .SetType(EffectType.effect_demobilizer)
                    .WithPropertyModifier(effectProperty);
                this.ApplyEffect(effectBuilder);
            }

            if (args.TotalAcidDamage > 0)
            {
                var effectBuilder = this.NewEffectBuilder();
                var _token = EffectToken.NewToken();

                effectBuilder
                    .WithToken(_token)
                    .SetType(EffectType.effect_acid_damage)
                    .WithDuration(TimeSpan.FromSeconds(10))
                    .WithDamagePerTick(args.TotalAcidDamage);
                    
                this.ApplyEffect(effectBuilder);
            }
        }

        protected override bool IsDetected(Unit target)
        {
            if (_lockHandler.IsLocked(target))
            {
                return true;
            }

            return base.IsDetected(target);
        }

        protected override void OnRemovedFromZone(IZone zone)
        {
            var remoteController = _modules.Value.FirstOrDefault(x => x is RemoteControllerModule) ;

            if (remoteController != null)
            {
                (remoteController as RemoteControllerModule).CloseAllChannels();
            }

            base.OnRemovedFromZone(zone);
        }

        private void InitComponents()
        {
            _components = new Lazy<IEnumerable<Item>>(() => Children.OfType<Item>().ToArray());
            _robotComponents = new Lazy<IEnumerable<RobotComponent>>(() => Components.OfType<RobotComponent>().ToArray());
            _modules = new Lazy<IEnumerable<Module>>(() => RobotComponents.SelectMany(c => c.Modules).ToArray());
            _activeModules = new Lazy<IEnumerable<ActiveModule>>(() => Modules.OfType<ActiveModule>().ToArray());
        }
    }
}
