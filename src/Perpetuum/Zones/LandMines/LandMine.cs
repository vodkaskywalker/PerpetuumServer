﻿using Perpetuum.ExportedTypes;
using Perpetuum.Modules.Weapons;
using Perpetuum.Players;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.ProximityProbes;
using Perpetuum.Zones.Teleporting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.LandMines
{
    public class LandMine : ProximityDeviceBase
    {
        private const int BeamDistance = 600;
        private readonly IntervalTimer probingInterval = new IntervalTimer(TimeSpan.FromSeconds(2));
        private readonly IntervalTimer gracePeriodInterval = new IntervalTimer(TimeSpan.FromSeconds(15));

        public int TriggerMass => ED.Options.GetOption<int>("triggerMass");

        protected override void OnUpdate(TimeSpan time)
        {
            if (!gracePeriodInterval.Passed)
            {
                _ = gracePeriodInterval.Update(time);

                return;
            }

            _ = gracePeriodInterval.Update(time);

            UnitDespawnHelper despawnHelper = GetDespawnHelper();

            _ = probingInterval.Update(time);

            if (!probingInterval.Passed)
            {
                return;
            }

            probingInterval.Reset();

            if (IsActive)
            {
                //detect
                List<Player> robotsNearMe = GetNoticedUnits();

                if (robotsNearMe.Exists(x => x.ActualMass > TriggerMass))
                {
                    robotsNearMe[0].Zone.CreateBeam(
                        BeamType.plant_bomb_explosion,//.timebomb_explosion,
                        builder => builder
                            .WithPosition(CurrentPosition)
                            .WithState(BeamState.Hit)
                            .WithVisibility(BeamDistance));

                    // There be explosion
                    IDamageBuilder damageBuilder = DamageInfo.Builder.WithAttacker(this)
                        .WithDamage(DamageType.Chemical, ED.Config.damage_chemical ?? 5000.0)
                        .WithDamage(DamageType.Explosive, ED.Config.damage_explosive ?? 5000.0)
                        .WithDamage(DamageType.Kinetic, ED.Config.damage_kinetic ?? 5000.0)
                        .WithDamage(DamageType.Thermal, ED.Config.damage_thermal ?? 5000.0)
                        .WithDamage(DamageType.Toxic, ED.Config.damage_toxic ?? 5000.0)
                        .WithOptimalRange(2)
                        .WithFalloff(ED.Config.item_work_range ?? 30.0)
                        .WithExplosionRadius(ED.Config.explosion_radius ?? 50.0);

                    robotsNearMe[0].Zone.DoAoeDamageAsync(damageBuilder);

                    OnDead(robotsNearMe[0]);
                }
            }

            if (despawnHelper == null)
            {
                Items.ItemPropertyModifier m = GetPropertyModifier(AggregateField.despawn_time);
                TimeSpan timespan = TimeSpan.FromMilliseconds((int)m.Value);
                SetDespawnTime(timespan);
                despawnHelper = GetDespawnHelper();
            }

            despawnHelper.Update(time, this);
        }

        #region probe functions

        [CanBeNull]
        public override List<Player> GetNoticedUnits()
        {
            return GetVisibleUnits().Select(v => v.Target).OfType<Player>().ToList();
        }

        protected override bool IsDetected(Unit target)
        {
            double range = ED.Config.item_work_range ?? 5.0;

            return IsInRangeOf3D(target, range);
        }

        public override void CheckDeploymentAndThrow(IZone zone, Position spawnPosition)
        {
            zone.Units.OfType<DockingBase>().WithinRange(spawnPosition, DistanceConstants.LANDMINE_DEPLOY_RANGE_FROM_BASE).Any().ThrowIfTrue(ErrorCodes.NotDeployableNearObject);
            zone.Units.OfType<Teleport>().WithinRange(spawnPosition, DistanceConstants.LANDMINE_DEPLOY_RANGE_FROM_TELEPORT).Any().ThrowIfTrue(ErrorCodes.TeleportIsInRange);
            zone.Units.OfType<ProximityDeviceBase>().WithinRange(spawnPosition, DistanceConstants.LANDMINE_DEPLOY_RANGE_FROM_LANDMINE).Any().ThrowIfTrue(ErrorCodes.TooCloseToOtherDevice);
        }

        #endregion
    }
}
