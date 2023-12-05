using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.StateMachines;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones.DamageProcessors;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.NpcSystem.AI;
using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.IndustrialTargetsManagement;
using Perpetuum.Zones.NpcSystem.ThreatManaging;
using Perpetuum.Zones.RemoteControl;
using Perpetuum.Zones.Scanning.Results;
using Perpetuum.Zones.Terrains.Materials.Minerals;
using System;
using System.Linq;
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.NpcSystem.AI.Behaviors;
using Perpetuum.Zones.Terrains.Materials.Plants;
using Perpetuum.Players;

namespace Perpetuum.Zones.NpcSystem
{
    public class SmartCreature : Creature
    {
        private const double AggroRange = 30;
        private const double BestComnatRangeModifier = 0.9;
        private const double BaseCallForHelpArmorThreshold = 0.2;
        private const int IndustrialScanRange = 30;
        private readonly ThreatManager threatManager;
        private readonly PseudoThreatManager pseudoThreatManager;
        private readonly IndustrialValueManager industrialValueManager;
        private readonly TimeKeeper debounceBodyPull = new TimeKeeper(TimeSpan.FromSeconds(2.5));
        private readonly TimeKeeper debounceLockChange = new TimeKeeper(TimeSpan.FromSeconds(2.5));
        private readonly IntervalTimer pseudoUpdateFreq = new IntervalTimer(TimeSpan.FromMilliseconds(650));
        private Lazy<int> maxCombatRange;
        private Lazy<int> optimalCombatRange;
        private TimeSpan lastHelpCalled;

        [CanBeNull]
        private ISmartCreatureGroup group;

        public StackFSM AI { get; private set; }

        public Behavior Behavior { get; set; }

        public double HomeRange { get; set; }

        public Position HomePosition { get; set; }

        public ThreatManager ThreatManager
        {
            get { return this.threatManager; }
        }

        public PseudoThreatManager PseudoThreatManager
        {
            get { return pseudoThreatManager; }
        }

        public IndustrialValueManager IndustrialValueManager
        {
            get { return industrialValueManager; }
        }

        public int BestCombatRange
        {
            get { return this.optimalCombatRange.Value; }
        }

        public int MaxCombatRange
        {
            get { return this.maxCombatRange.Value; }
        }

        public bool IsInHomeRange
        {
            get { return CurrentPosition.IsInRangeOf2D(HomePosition, HomeRange); }
        }

        public int MaxIndustrialRange
        {
            get
            {
                return IndustrialScanRange;
            }
        }

        public ISmartCreatureGroup Group
        {
            get { return group; }
        }

        public virtual bool IsStationary
        {
            get { return MaxSpeed.IsZero(); }
        }

        public virtual double CallForHelpArmorThreshold => BaseCallForHelpArmorThreshold;

        public bool CallForHelp { private get; set; }

        public NpcBossInfo BossInfo { get; set; }

        public SmartCreature()
        {
            RecalculateMaxCombatRange();
            RecalculateOptimalCombatRange();
            threatManager = new ThreatManager();
            AI = new StackFSM();
            pseudoThreatManager = new PseudoThreatManager();
            industrialValueManager = new IndustrialValueManager();
        }

        public void LookingForHostiles()
        {
            foreach (var visibility in GetVisibleUnits())
            {
                AddBodyPullThreat(visibility.Target);
            }
        }

        public void LookingForMiningTargets()
        {
            IndustrialValueManager.Clear();

            var area = Zone.CreateArea(CurrentPosition, IndustrialScanRange);

            var availableMaterialTypes = Zone.Terrain.GetAvailableMineralTypes();

            foreach (MaterialType materialType in availableMaterialTypes)
            {
                var builder = MineralScanResultBuilder.Create(Zone, materialType);

                builder.ScanArea = area;
                builder.ScanAccuracy = 100.0;

                var result = builder.Build();

                MineralLayer mineralLayer = Zone.Terrain.GetMaterialLayer(materialType) as MineralLayer;

                if (mineralLayer != null)
                {
                    if (result.FoundAny)
                    {
                        var valuablePositions = result.Area
                            .GetPositions()
                            .Where(x => mineralLayer.HasMineral(x))
                            .Select(x=>Zone.FixZ(x))
                            .Where(x => x.IsInRangeOf3D(this.PositionWithHeight, this.BestCombatRange));

                        foreach (var valuablePosition in valuablePositions)
                        {
                            var mineralNode = mineralLayer.GetNode(valuablePosition);

                            IndustrialValueManager.GetOrAddIndustrialTargetWithValue(valuablePosition.Center, IndustrialValueType.Mineral, mineralNode.GetValue(valuablePosition));
                        }
                    }
                }
            }
        }

        public void LookingForHarvestingTargets()
        {
            IndustrialValueManager.Clear();

            var area = Zone.CreateArea(CurrentPosition, IndustrialScanRange);

            var availablePlantTypes = Zone.Terrain.GetAvailablePlantTypes();

            foreach (PlantType plantType in availablePlantTypes)
            {
                var plantsCounter = Zone.CountPlantsInArea(plantType, area);

                if (plantsCounter > 0)
                {
                    var valuablePositions = Zone.GetPlantPositionsInArea(plantType, area)
                        .Select(x => Zone.FixZ(x))
                        .Where(x => x.IsInRangeOf2D(this.CurrentPosition, this.BestCombatRange));

                    foreach (var valuablePosition in valuablePositions)
                    {
                        var plant = Zone.Terrain.Plants.GetValue(valuablePosition.intX, valuablePosition.intY);

                        if (plant.material > 0)
                        {
                            IndustrialValueManager.GetOrAddIndustrialTargetWithValue(valuablePosition.Center, IndustrialValueType.Mineral, plant.material);
                        }                       
                    }
                }
            }
        }

        public void AddPseudoThreat(Unit hostile)
        {
            pseudoThreatManager.AddOrRefreshExisting(hostile);
        }

        public void SetGroup(ISmartCreatureGroup group)
        {
            this.group = group;
        }

        public void RecalculateOptimalCombatRange()
        {
            optimalCombatRange = new Lazy<int>(CalculateCombatRange);
        }

        public void RecalculateMaxCombatRange()
        {
            maxCombatRange = new Lazy<int>(CalculateMaxCombatRange);
        }

        protected override void OnPropertyChanged(ItemProperty property)
        {
            base.OnPropertyChanged(property);

            switch (property.Field)
            {
                case AggregateField.locking_range:
                    {
                        RecalculateOptimalCombatRange();
                        RecalculateMaxCombatRange();

                        break;
                    }
                case AggregateField.armor_current:
                    {
                        var percentage = Armor.Ratio(ArmorMax);

                        if (percentage <= CallForHelpArmorThreshold)
                        {
                            CallingForHelp();
                        }

                        break;
                    }
            }
        }

        protected override void OnUnitLockStateChanged(Lock @lock)
        {
            if (!debounceLockChange.Expired)
            {
                return;
            }

            var unitLock = @lock as UnitLock;

            if (unitLock == null)
            {
                return;
            }

            if (unitLock.Target != this)
            {
                return;
            }

            if (unitLock.State != LockState.Locked)
            {
                return;
            }

            var threatValue = unitLock.Primary
                ? Threat.LOCK_PRIMARY
                : Threat.LOCK_SECONDARY;

            AddThreat(unitLock.Owner, new Threat(ThreatType.Lock, threatValue), true);
            debounceLockChange.Reset();
        }

        protected override void OnUnitTileChanged(Unit target)
        {
            if (!debounceBodyPull.Expired)
            {
                return;
            }

            AddBodyPullThreat(target);
            debounceBodyPull.Reset();
        }

        private void AddBodyPullThreat(Unit enemy)
        {
            if (!IsHostile(enemy))
            {
                return;
            }

            var helper = new BodyPullThreatHelper(this);

            enemy.AcceptVisitor(helper);
        }

        private int CalculateCombatRange()
        {
            double range = (int)ActiveModules
                .Where(m => m.IsRanged)
                .Select(module => module.OptimalRange)
                .Concat(new[] { MaxTargetingRange })
                .Min();

            range *= BestComnatRangeModifier;
            range = Math.Max(3, range);

            return (int)range;
        }

        private int CalculateMaxCombatRange()
        {
            double range = ActiveModules.Where(m => m.IsRanged)
                         .Select(module => (int)(module.OptimalRange + module.Falloff))
                         .Max();

            range = Math.Max(3, range);

            return (int)range;
        }

        public bool IsInAggroRange(Unit target)
        {
            return IsStationary || IsInRangeOf3D(target, AggroRange);
        }

        public void AddThreat(Unit hostile, Threat threat, bool spreadToGroup)
        {
            if (hostile.IsPlayer())
            {
                BossInfo?.OnAggro(hostile as Player);
            }

            threatManager.GetOrAddHostile(hostile).AddThreat(threat);

            RemovePseudoThreat(hostile);

            if (!spreadToGroup)
            {
                return;
            }

            var group = Group;
            if (@group == null)
            {
                return;
            }

            var multipliedThreat = Threat.Multiply(threat, 0.5);

            foreach (var member in @group.Members)
            {
                if (member == this)
                {
                    continue;
                }

                member.AddThreat(hostile, multipliedThreat, false);
            }
        }

        /// <summary>
        /// This determines if threat can be added to a target based on the following:
        ///  - Is the target already on the threat manager
        ///  - Or is the npc non-passive and the Threat is of some defined type
        /// </summary>
        /// <param name="target">Unit target</param>
        /// <param name="threat">Threat threat</param>
        /// <returns>If the target can be a threat</returns>
        public bool CanAddThreatTo(Unit target, Threat threat)
        {
            if (threatManager.Contains(target))
            {
                return true;
            }

            if (Behavior.Type == BehaviorType.Passive)
            {
                return false;
            }

            return threat.type != ThreatType.Undefined;
        }

        public void AddAssistThreat(Unit assistant, Unit target, Threat threat)
        {
            if (!threatManager.Contains(target))
            {
                return;
            }

            if (!CanAddThreatTo(assistant, threat))
            {
                return;
            }

            AddThreat(assistant, threat, true);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        protected override void OnTileChanged()
        {
            base.OnTileChanged();
            LookingForHostiles();
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            States.Aggressive = Behavior.Type == BehaviorType.Aggressive;

            base.OnEnterZone(zone, enterType);

            if (this is SentryTurret)
            {
                AI.Push(new SentryTurretCombatAI(this));
            }
            else if (this is IndustrialTurret)
            {
                if ((this as IndustrialTurret).TurretType == TurretType.Mining)
                {
                    AI.Push(new MiningIndustrialTurretAI(this));
                }
                else
                {
                    AI.Push(new HarvestingIndustrialTurretAI(this));
                }
            }
            else if (IsStationary)
            {
                AI.Push(new StationaryIdleAI(this));
            }
            else
            {
                AI.Push(new IdleAI(this));
            }
        }

        protected override void OnDamageTaken(Unit source, DamageTakenEventArgs e)
        {
            base.OnDamageTaken(source, e);

            var player = Zone.ToPlayerOrGetOwnerPlayer(source);

            if (player == null)
            {
                return;
            }

            BossInfo?.OnDamageTaken(this, player);
            AddThreat(player, new Threat(ThreatType.Damage, e.TotalDamage * 0.9), true);
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);

            AI.Update(time);

            UpdatePseudoThreats(time);
        }

        private void RemovePseudoThreat(Unit hostile)
        {
            pseudoThreatManager.Remove(hostile);
        }

        private void UpdatePseudoThreats(TimeSpan time)
        {
            pseudoUpdateFreq.Update(time);

            if (pseudoUpdateFreq.Passed)
            {
                pseudoThreatManager.Update(pseudoUpdateFreq.Elapsed);
                pseudoUpdateFreq.Reset();
            }
        }

        private void CallingForHelp()
        {
            if (!CallForHelp)
            {
                return;
            }

            if (!GlobalTimer.IsPassed(ref lastHelpCalled, TimeSpan.FromSeconds(5)))
            {
                return;
            }

            var group = Group;

            if (group == null)
            {
                return;
            }

            foreach (var member in group.Members.Where(flockMember => flockMember != this))
            {
                member.HelpingFor(this);
            }
        }

        private void HelpingFor(SmartCreature caller)
        {
            if (Armor.Ratio(ArmorMax) < CallForHelpArmorThreshold)
            {
                return;
            }

            ThreatManager.Clear();

            foreach (var hostile in caller.ThreatManager.Hostiles)
            {
                AddThreat(hostile.Unit, new Threat(ThreatType.Undefined, hostile.Threat), true);
            }
        }
    }
}
