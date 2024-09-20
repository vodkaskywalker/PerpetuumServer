﻿using Perpetuum.Modules.EffectModules;
using Perpetuum.Modules.Weapons;
using Perpetuum.Robots;
using Perpetuum.Units;
using Perpetuum.Zones.CombatLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Perpetuum.Zones.DamageProcessors
{
    public class DamageProcessor
    {
        private readonly Unit _unit;
        private Lazy<ShieldGeneratorModule> _shield;
        private readonly Queue<DamageInfo> _damageInfos = new Queue<DamageInfo>();
        private bool _processing;

        public CombatEventHandler<DamageTakenEventArgs> DamageTaken { private get; set; }

        public DamageProcessor(Unit unit)
        {
            _unit = unit;
            OnRequipUnit();
        }

        public void OnRequipUnit()
        {
            _shield = new Lazy<ShieldGeneratorModule>(() =>
            {
                Robot robot = _unit as Robot;
                return robot?.Modules.OfType<ShieldGeneratorModule>().FirstOrDefault();
            });
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!_unit.InZone || _unit.IsAttackable != ErrorCodes.NoError || _unit.States.Dead || _unit.IsInvulnerable)
            {
                return;
            }

            lock (_damageInfos)
            {
                if (!_processing)
                {
                    _processing = true;
                    _ = Task.Run(() => ProcessFirstDamage(damageInfo)).ContinueWith(t => _processing = false);
                    return;
                }

                _damageInfos.Enqueue(damageInfo);
            }
        }

        private void ProcessFirstDamage(DamageInfo info)
        {
            while (true)
            {
                ProcessDamage(info);

                lock (_damageInfos)
                {
                    if (_damageInfos.Count == 0)
                    {
                        return;
                    }

                    info = _damageInfos.Dequeue();
                }
            }
        }

        private void ProcessDamage(DamageInfo damageInfo)
        {
            if (!_unit.InZone || _unit.IsAttackable != ErrorCodes.NoError || _unit.States.Dead || _unit.IsInvulnerable)
            {
                return;
            }

            double totalDamage = 0.0;
            double totalKers = 0.0;
            double totalAbsorbedDamage = 0.0;

            foreach (Damage damage in damageInfo.CalculateDamages(_unit))
            {
                double partialDamage = damage.type == DamageType.Electric
                    ? CalculateAbsorbedDamage(damage.value, true, ref totalAbsorbedDamage)
                    : CalculateAbsorbedDamage(damage.value, false, ref totalAbsorbedDamage);
                if (partialDamage <= 0.0)
                {
                    continue;
                }

                double resist = _unit.GetResistByDamageType(damage.type);
                partialDamage -= partialDamage * resist;

                double kers = CalculateKersValue(damage.type, partialDamage);
                if (kers > 0.0)
                {
                    _unit.Core += kers;
                    totalKers += kers;
                }

                totalDamage += partialDamage;
            }

            CombatEventHandler<DamageTakenEventArgs> h = DamageTaken;
            if (h == null)
            {
                return;
            }

            DamageTakenEventArgs e = new DamageTakenEventArgs
            {
                TotalDamage = totalDamage,
                TotalCoreDamage = totalAbsorbedDamage,
                TotalKers = totalKers,
                IsCritical = damageInfo.IsCritical,
                IsKillingBlow = false
            };

            h(damageInfo.attacker, e);
        }

        private double CalculateAbsorbedDamage(double damage, bool isPenetrating, ref double absorbed)
        {
            if (_shield.Value == null || !_unit.HasShieldEffect)
            {
                return damage;
            }

            double absorbtionModifier = _shield.Value.AbsorbtionModifier;
            if (absorbtionModifier <= 0.0)
            {
                return damage;
            }

            double currCore = _unit.Core;
            if (currCore < 1.0)
            {
                return damage;
            }

            // damage = 100
            // absorb = 1.2 / 0.8
            double coreDamage = damage * absorbtionModifier;
            if (currCore < coreDamage)
            {
                damage -= currCore / absorbtionModifier;
                absorbed += currCore;
                currCore = 0.0;
            }
            else
            {
                damage = isPenetrating
                    ? coreDamage
                    : 0.0;
                absorbed += coreDamage;
                currCore -= coreDamage;
            }

            _unit.Core = currCore;

            return damage;
        }

        private double CalculateKersValue(DamageType damageType, double damage)
        {
            double kersModifier = _unit.GetKersByDamageType(damageType);

            if (Math.Abs(kersModifier - 1.0) < double.Epsilon)
            {
                return 0.0;
            }

            double kers = damage * kersModifier;

            if (kers <= 0.0)
            {
                return 0.0;
            }

            double kersMod = (Math.Sin(_unit.Core / _unit.CoreMax * Math.PI) / 2) + 0.5;
            return kers * kersMod;
        }
    }
}