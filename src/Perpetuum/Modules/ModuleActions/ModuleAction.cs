using Perpetuum.Modules.Weapons;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones;
using System;
using System.Threading.Tasks;

namespace Perpetuum.Modules.ModuleActions
{
    public class ModuleAction : ILockVisitor
    {
        public WeaponModule Weapon { get; private set; }

        public ModuleAction(WeaponModule weapon)
        {
            Weapon = weapon;
        }

        public virtual void DoAction()
        {
            Weapon.ParentRobot.HasShieldEffect.ThrowIfTrue(ErrorCodes.ShieldIsActive);

            var currentLock = Weapon.GetLock();

            currentLock?.AcceptVisitor(this);
        }

        public void VisitLock(Lock @lock)
        {
        }

        public virtual void VisitUnitLock(UnitLock unitLock)
        {
            var victim = unitLock.Target;

            victim.InZone.ThrowIfFalse(ErrorCodes.TargetNotFound);
            victim.States.Dead.ThrowIfTrue(ErrorCodes.TargetIsDead);

            var err = victim.IsAttackable;

            if (err != ErrorCodes.NoError)
            {
                throw new PerpetuumException(err);
            }

            victim.IsInvulnerable.ThrowIfTrue(ErrorCodes.TargetIsInvulnerable);
            Weapon.ConsumeAmmo();

            var result = Weapon.GetLineOfSight(victim);

            if (result.hit)
            {
                DoDamageToPosition(result.position);
                Weapon.OnError(ErrorCodes.LOSFailed);

                return;
            }

            var distance = Weapon.ParentRobot.GetDistance(victim);
            var bulletTime = Weapon.GetAmmo().BulletTime;
            var flyTime = (int)((distance / bulletTime) * 1000);
            var beamTime = (int)Math.Max(flyTime, Weapon.CycleTime.TotalMilliseconds);
            var miss = Weapon.CheckAccuracy(victim);

            if (miss)
            {
                Weapon.CreateBeam(victim, BeamState.Miss, beamTime, bulletTime);
                Weapon.OnError(ErrorCodes.AccuracyCheckFailed);

                return;
            }

            var delay = Weapon.CreateBeam(victim, BeamState.Hit, beamTime, bulletTime);

            flyTime += delay;

            var builder = Weapon.GetDamageBuilder();

            Task.Delay(flyTime).ContinueWith(t => victim.TakeDamage(builder.Build()));
        }

        public virtual void VisitTerrainLock(TerrainLock terrainLock)
        {
            var location = terrainLock.Location;

            Weapon.ConsumeAmmo();

            var blockingInfo = Weapon?.ParentRobot?.Zone?.Terrain.Blocks.GetValue(terrainLock.Location) ?? BlockingInfo.None;

            location = location.AddToZ(Math.Min(blockingInfo.Height, 20));

            var losResult = Weapon.GetLineOfSight(location);

            if (losResult.hit && !location.IsEqual2D(losResult.position))
            {
                location = losResult.position;
                Weapon.OnError(ErrorCodes.LOSFailed);
            }

            DoDamageToPosition(location);
        }

        private void DoDamageToPosition(Position location)
        {
            var distance = Weapon.ParentRobot.CurrentPosition.TotalDistance3D(location);
            var bulletTime = Weapon.GetAmmo().BulletTime;
            var flyTime = (int)((distance / bulletTime) * 1000);
            var beamTime = (int)Math.Max(flyTime, Weapon.CycleTime.TotalMilliseconds);

            flyTime += Weapon.CreateBeam(location, BeamState.Hit, beamTime, bulletTime);

            var damage = Weapon.GetDamageBuilder().Build().CalculatePlantDamages();

            if (damage <= 0.0)
            {
                return;
            }

            var zone = Weapon.Zone;

            if (zone == null)
            {
                return;
            }

            Task.Delay(flyTime).ContinueWith(t => DealDamageToPosition(zone, location, damage));
        }

        private static void DealDamageToPosition(IZone zone, Position location, double damage)
        {
            using (new TerrainUpdateMonitor(zone))
            {
                zone.DamageToPlantOnArea(Area.FromRadius(location, 1), damage / 2.0);
            }
        }
    }
}
