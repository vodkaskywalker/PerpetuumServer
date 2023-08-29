using System;
using System.Linq;
using Perpetuum.Deployers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Players;
using Perpetuum.Units;

namespace Perpetuum.Zones.ProximityProbes
{
    public class ProximityProbeDeployer : ItemDeployer
    {
        private readonly IEntityServices entityServices;

        public ProximityProbeDeployer(IEntityServices entityServices) : base(entityServices)
        {
            this.entityServices = entityServices;
        }

        protected override Unit CreateDeployableItem(IZone zone, Position spawnPosition, Player player)
        {
            zone.Configuration.Protected.ThrowIfTrue(ErrorCodes.OnlyUnProtectedZonesAllowed);

            var corporation = player.Character.GetPrivateCorporationOrThrow();

            var maxProbes = corporation.GetMaximumProbeAmount();
            corporation.GetProximityProbeEids().Count().ThrowIfGreaterOrEqual(maxProbes, ErrorCodes.MaximumAmountOfProbesReached);

            var probe = (ProximityDeviceBase)entityServices.Factory.CreateWithRandomEID(DeployableItemEntityDefault);
            probe.CheckDeploymentAndThrow(zone, spawnPosition); //Enforce min-distance separations
            probe.Owner = corporation.Eid;
            var zoneStorage = zone.Configuration.GetStorage();
            probe.Parent = zoneStorage.Eid;
            probe.Save();

            zone.UnitService.AddUserUnit(probe,spawnPosition);

            var initialMembers = corporation.GetMembersWithAnyRoles(CorporationRole.CEO, CorporationRole.DeputyCEO).Select(cm => cm.character).ToList();
            initialMembers.Add(player.Character);
            
            probe.Init( initialMembers.Distinct() );
            probe.SetDespawnTime(this.ProximityProbeDespawnTime);
            return probe;
        }

        private TimeSpan ProximityProbeDespawnTime
        {
            get
            {
                var m = GetPropertyModifier(AggregateField.despawn_time);
                return TimeSpan.FromMilliseconds((int)m.Value);
            }
        }
    }
}