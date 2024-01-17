using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Services.Looting;
using Perpetuum.Units;
using Perpetuum.Zones.NpcSystem;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System;
using System.Security.Policy;
using Perpetuum.Services.Standing;

namespace Perpetuum.Zones.RemoteControl
{
    public class IndustrialTurret : RemoteControlledCreature
    {
        public TurretType TurretType { get; private set; }

        public IndustrialTurret(IStandingHandler standingHandler)
            : base(standingHandler)
        {
        }

        public void SetTurretType(TurretType turretType)
        {
            TurretType = turretType;
        }

        protected override bool IsHostileFor(Unit unit)
        {
            return unit.IsHostile(this);
        }

        public override bool IsHostile(Player player)
        {
            return false;
        }

        internal override bool IsHostile(Npc npc)
        {
            return false;
        }

        protected override void OnBeforeRemovedFromZone(IZone zone)
        {
            EjectCargo(zone);
            base.OnBeforeRemovedFromZone(zone);
        }

        public void EjectCargo(IZone zone)
        {
            using (var scope = Db.CreateTransaction())
            {
                try
                {
                    var robotInventory = GetContainer();

                    Debug.Assert(robotInventory != null);

                    var lootItems = new List<LootItem>();

                    foreach (var item in robotInventory
                        .GetItems()
                        .Where(i => !i.ED.AttributeFlags.NonStackable))
                    {
                        if (item.Quantity > 0)
                        {
                            lootItems.Add(
                                LootItemBuilder
                                    .Create(item.Definition)
                                    .SetQuantity(item.Quantity)
                                    .SetRepackaged(item.ED.AttributeFlags.Repackable)
                                    .Build());
                        }

                        robotInventory.RemoveItemOrThrow(item);
                        Repository.Delete(item);
                    }

                    if (lootItems.Count > 0)
                    {
                        var lootContainer = LootContainer.Create()
                            .AddLoot(lootItems)
                            .BuildAndAddToZone(zone, CurrentPosition);
                    }

                    this.Save();

                    scope.Complete();
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }
    }
}
