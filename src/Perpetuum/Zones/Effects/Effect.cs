using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.Weapons.Damages;
using Perpetuum.Timers;
using Perpetuum.Units;

namespace Perpetuum.Zones.Effects
{
    public delegate void EffectEventHandler(Effect effect);
    public delegate void EffectEventHandler<in T>(Effect effect,T arg);

    public delegate Effect EffectFactory(EffectType effectType);

    /// <summary>
    /// Base class for an effect
    /// </summary>
    public class Effect 
    {
        private readonly IntervalTimer _tickTimer = new IntervalTimer(TimeSpan.FromSeconds(2));
        protected IEnumerable<ItemPropertyModifier> propertyModifiers = new ItemPropertyModifier[] { };
        
        public int Id { get; internal set; }

        public EffectType Type { get; internal set; }

        public EffectCategory Category { get; internal set; }

        public EffectToken Token { get; internal set; }

        public Unit Owner { get; internal set; }

        public Unit Source { get; set; }

        public bool EnableModifiers { get; set; }

        public bool Display { get; set; }

        public bool IsAura { get; set; }

        public double DamagePerTick { get; internal set; }

        public event EffectEventHandler Removed;

        public IEnumerable<ItemPropertyModifier> PropertyModifiers
        {
            get
            {
                return EffectHelper.GetEffectDefaultModifiers(Type).Concat(propertyModifiers);
            }

            set { propertyModifiers = value; }
        }

        public IntervalTimer Timer { get; set; }

        public Effect()
        {
            Timer = null;
            EnableModifiers = true;
        }

        public void Update(TimeSpan time)
        {
            if (Timer != null)
            {
                Timer.Update(time);

                if (Timer.Passed)
                {
                    OnRemoved();

                    return;
                }
            }

            _tickTimer.Update(time).IsPassed(OnTick);
        }

        public void ApplyTo(ref ItemPropertyModifier propertyModifier,AggregateField modifierField)
        {
            if (!EnableModifiers)
            {
                return;
            }

            foreach (var modifier in PropertyModifiers.Where(pp => pp.Field == modifierField))
            {
                modifier.Modify(ref propertyModifier);
            }
        }

        public void AppendToStream(BinaryStream stream)
        {
            stream.AppendInt(Id);
            stream.AppendInt((int)Type);
            stream.AppendLong(Owner.Eid);

            var sourceEid = Source?.Eid ?? 0L;

            stream.AppendLong(sourceEid);

            var timer = Timer;

            if (timer != null)
            {
                stream.AppendInt((int)timer.Interval.TotalMilliseconds);
                stream.AppendInt((int)timer.Elapsed.TotalMilliseconds);
            }
            else
            {
                stream.AppendInt(0);
                stream.AppendInt(0);
            }

            stream.AppendInt(PropertyModifiers.Count());

            foreach (var property in PropertyModifiers)
            {
                property.AppendToPacket(stream);
            }
        }

        protected virtual void OnTick() { }

        protected virtual void OnRemoved()
        {
            Removed?.Invoke(this);
        }
    }
}