using Perpetuum.Modules.Weapons;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones;
using System;
using System.Threading.Tasks;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Modules.ModuleActions
{
    public class ArtilleryModuleAction : ILockVisitor
    {
        private const int BeamDistance = 600;
        private readonly ArtilleryWeaponModule _weapon;

        public ArtilleryModuleAction(ArtilleryWeaponModule weapon)
        {
            _weapon = weapon;
        }

        public void DoAction()
        {
            _weapon.ParentRobot.HasShieldEffect.ThrowIfTrue(ErrorCodes.ShieldIsActive);

            var currentLock = _weapon.GetLock();

            currentLock?.AcceptVisitor(this);
        }

        public void VisitLock(Lock @lock)
        {
        }

        public void VisitUnitLock(UnitLock unitLock)
        {
            var victimPosition = unitLock.Target.CurrentPosition;
            var positionNearby = victimPosition.GetRandomPositionInRange2D(3, 8);
            var terrainLock = new TerrainLock(this._weapon.ParentRobot, positionNearby);
            
            VisitTerrainLock(terrainLock);
        }

        public void VisitTerrainLock(TerrainLock terrainLock)
        {
            var location = terrainLock.Location;

            _weapon.ConsumeAmmo();

            var blockingInfo = _weapon?.ParentRobot?.Zone?.Terrain.Blocks.GetValue(terrainLock.Location) ?? BlockingInfo.None;

            location = location.AddToZ(Math.Min(blockingInfo.Height, 20));

            DoDamageToPosition(location);
        }

        private void DoDamageToPosition(Position location)
        {
            _weapon.Zone.CreateBeam(
                        BeamType.sap_scanner_beam,
                        builder => builder
                            .WithPosition(location)
                            .WithState(BeamState.AlignToTerrain)
                            .WithDuration(1000));

            var distance = _weapon.ParentRobot.CurrentPosition.TotalDistance3D(location);
            var bulletTime = _weapon.GetAmmo().BulletTime;
            var flyTime = (int)((distance / bulletTime) * 1000);
            var beamTime = (int)Math.Max(flyTime, _weapon.CycleTime.TotalMilliseconds);

            flyTime += _weapon.CreateBeam(location, BeamState.Hit, beamTime, bulletTime, 20);

            var damage = GetExplosionDamageBuilder(location);

            var zone = _weapon.Zone;

            if (zone == null)
            {
                return;
            }

            Task.Delay(flyTime).ContinueWith(t => DealDamageToPosition(zone, location, damage));
        }

        private static void DealDamageToPosition(IZone zone, Position location, IDamageBuilder damageBuilder)
        {
            using (new TerrainUpdateMonitor(zone))
            {
                zone.CreateBeam(
                        BeamType.plant_bomb_explosion,
                        builder => builder
                            .WithPosition(location)
                            .WithState(BeamState.Hit)
                            .WithVisibility(BeamDistance));
                zone.DoAoeDamageAsync(damageBuilder);
            }
        }

        private IDamageBuilder GetExplosionDamageBuilder(Position location)
        {
            var radius = 10.0;
            var damageBuilder = DamageInfo.Builder
                .WithAttackerWithoutPosition(_weapon.ParentRobot)
                .WithSourcePosition(location)
                .WithOptimalRange(1)
                .WithFalloff(radius)
                .WithExplosionRadius(radius);
            var ammo = (ArtilleryWeaponAmmo)_weapon.GetAmmo();
            var damages = ammo.GetCleanDamages();

            damageBuilder.WithDamages(damages);

            return damageBuilder;
        }
    }
}
