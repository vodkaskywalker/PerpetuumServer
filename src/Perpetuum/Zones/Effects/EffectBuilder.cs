using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.IDGenerators;
using Perpetuum.Items;
using Perpetuum.Modules.Weapons.Damages;
using Perpetuum.Timers;
using Perpetuum.Units;

namespace Perpetuum.Zones.Effects
{
    public class EffectBuilder
    {
        private readonly EffectFactory _effectFactory;
        private static readonly IIDGenerator<int> _idGenerator = IDGenerator.CreateIntIDGenerator();
        private EffectType _type;
        private Unit _owner;
        private Unit _source;
        private TimeSpan? _duration;
        private double? _durationModifier;
        private readonly IList<ItemPropertyModifier> _propertyModifiers = new List<ItemPropertyModifier>();
        private bool _enableModifiers = true;
        private double _corePerTick;
        private double? _radius;
        private double _radiusModifier = 1.0;
        private long _corporationEid;
        private EffectInfo _info;
        private EffectTargetSelector _targetSelector;
        private double _damagePerTick;

        public delegate EffectBuilder Factory();

        public Unit Owner => _owner;

        public EffectToken Token { get; private set; } = EffectToken.NewToken();

        public EffectBuilder(EffectFactory effectFactory)
        {
            _effectFactory = effectFactory;
        }

        public EffectBuilder SetOwnerToSource()
        {
            _source = Owner;

            return this;
        }

        public EffectBuilder SetType(EffectType type)
        {
            _type = type;

            return this;
        }

        public EffectBuilder WithCorePerTick(double corePerTick)
        {
            _corePerTick = corePerTick;

            return this;
        }

        public EffectBuilder WithCorporationEid(long corporationEid)
        {
            _corporationEid = corporationEid;

            return this;
        }

        public EffectBuilder WithRadius(double radius)
        {
            _radius = radius;

            return this;
        }

        public EffectBuilder WithRadiusModifier(double radiusModifier)
        {
            _radiusModifier = radiusModifier;

            return this;
        }

        public EffectBuilder EnableModifiers(bool state)
        {
            _enableModifiers = state;

            return this;
        }

        public EffectBuilder WithOwner(Unit owner)
        {
            _owner = owner;

            return this;
        }

        public EffectBuilder SetSource(Unit source)
        {
            _source = source;

            return this;
        }

        public EffectBuilder WithDuration(TimeSpan duration)
        {
            if (duration > TimeSpan.Zero)
            {
                _duration = duration;
            }
            return this;
        }

        public EffectBuilder WithDurationModifier(double modifier)
        {
            _durationModifier = modifier;

            return this;
        }

        public EffectBuilder WithPropertyModifier(ItemPropertyModifier propertyModifier)
        {
            _propertyModifiers.Add(propertyModifier);

            return this;
        }

        public EffectBuilder WithToken(EffectToken token)
        {
            Token = token;

            return this;
        }

        public EffectBuilder WithDuration(int duration)
        {
            return WithDuration(TimeSpan.FromMilliseconds(duration));
        }

        public EffectBuilder WithPropertyModifiers(IEnumerable<ItemPropertyModifier> propertyModifiers)
        {
            foreach (var propertyModifier in propertyModifiers)
            {
                WithPropertyModifier(propertyModifier);
            }

            return this;
        }

        public EffectBuilder WithTargetSelector(EffectTargetSelector selector)
        {
            _targetSelector = selector;

            return this;
        }

        public EffectBuilder WithDamagePerTick(double damagePerTick)
        {
            _damagePerTick = damagePerTick;

            return this;
        }

        public Effect Build()
        {
            var effect = _effectFactory(_type);

            if (_info == null)
            {
                _info = EffectHelper.GetEffectInfo(_type);
            }

            effect.Display = _info.Display;
            effect.IsAura = _info.isAura;

            var corporation = effect as CorporationEffect;

            corporation?.SetCorporationEid(_corporationEid);

            if (effect is AuraEffect aura)
            {
                aura.Radius = _radius ?? (_info.auraRadius * _radiusModifier);
                aura.TargetSelector = _targetSelector;
            }

            var cot = effect as CoTEffect;

            cot?.SetCorePerTick(_corePerTick);
            effect.Id = _idGenerator.GetNextID();
            effect.Type = _type;
            effect.Category = _info.category;
            effect.Token = Token;
            effect.Owner = _owner;
            effect.Source = _source;
            effect.PropertyModifiers = _propertyModifiers.ToArray();
            effect.EnableModifiers = _enableModifiers;
            effect.DamagePerTick = _damagePerTick;

            var duration = _duration ?? TimeSpan.FromMilliseconds(_info.duration);

            if (_durationModifier != null)
            {
                duration = TimeSpan.FromMilliseconds(duration.TotalMilliseconds * ((double)_durationModifier));
            }

            if (duration > TimeSpan.Zero)
            {
                effect.Timer = new IntervalTimer(duration);
            }

            return effect;
        }
    }
}