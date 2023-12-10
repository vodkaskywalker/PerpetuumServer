using Perpetuum.Modules.Weapons;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones;
using System;
using System.Threading.Tasks;
using Perpetuum.ExportedTypes;
using Perpetuum.Modules.Weapons.Damages;
using Perpetuum.Modules.Weapons.Amunition;

namespace Perpetuum.Modules.ModuleActions
{
    public class ArtilleryModuleAction : ModuleAction
    {
        private const int BeamDistance = 600;

        public ArtilleryModuleAction(ArtilleryWeaponModule weapon) : base(weapon)
        {
        }

        public override void VisitUnitLock(UnitLock unitLock)
        {
            var victimPosition = unitLock.Target.CurrentPosition;
            var positionNearby = victimPosition.GetRandomPositionInRange2D(3, 8);
            var terrainLock = new TerrainLock(this.Weapon.ParentRobot, positionNearby);
            
            VisitTerrainLock(terrainLock);
        }

        public override void VisitTerrainLock(TerrainLock terrainLock)
        {
            var location = terrainLock.Location;

            Weapon.ConsumeAmmo();

            var blockingInfo = Weapon?.ParentRobot?.Zone?.Terrain.Blocks.GetValue(terrainLock.Location) ?? BlockingInfo.None;

            location = location.AddToZ(Math.Min(blockingInfo.Height, 20));

            DoDamageToPosition(location);
        }

        private void DoDamageToPosition(Position location)
        {
            Weapon.Zone.CreateBeam(
                        BeamType.teleport_storm,
                        builder => builder
                            .WithPosition(location)
                            .WithState(BeamState.AlignToTerrain)
                            .WithDuration(100));

            var distance = Weapon.ParentRobot.CurrentPosition.TotalDistance3D(location);
            var bulletTime = Weapon.GetAmmo().BulletTime;
            var flyTime = (int)((distance / bulletTime) * 1000);
            var beamTime = (int)Math.Max(flyTime, Weapon.CycleTime.TotalMilliseconds);

            flyTime += Weapon.CreateBeam(location, BeamState.Hit, beamTime, bulletTime);

            var damage = GetExplosionDamageBuilder(location);

            var zone = Weapon.Zone;

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
                .WithAttackerWithoutPosition(Weapon.ParentRobot)
                .WithSourcePosition(location)
                .WithOptimalRange(1)
                .WithFalloff(radius)
                .WithExplosionRadius(radius);
            var ammo = (ArtilleryWeaponAmmo)Weapon.GetAmmo();
            var damages = ammo.GetCleanDamages();

            damageBuilder.WithDamages(damages);

            return damageBuilder;
        }
    }
}
