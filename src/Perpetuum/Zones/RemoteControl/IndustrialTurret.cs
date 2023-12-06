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

namespace Perpetuum.Zones.RemoteControl
{
    public class IndustrialTurret : RemoteControlledTurret
    {
        public TurretType TurretType { get; private set; }

        public IndustrialTurret()
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

        protected override void OnRemovedFromZone(IZone zone)
        {
            using (var scope = Db.CreateTransaction())
            {
                EnlistTransaction();

                try
                {
                    var robotInventory = GetContainer();

                    Debug.Assert(robotInventory != null);

                    var lootItems = new List<LootItem>();

                    foreach (var item in robotInventory.GetItems(true).Where(i => i is VolumeWrapperContainer))
                    {
                        var wrapper = item as VolumeWrapperContainer;

                        if (wrapper == null)
                        {
                            continue;
                        }

                        lootItems.AddRange(wrapper.GetLootItems());
                        wrapper.SetAllowDelete();
                        Repository.Delete(wrapper);
                    }

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
                        else
                        {
                            robotInventory.RemoveItemOrThrow(item);
                            Repository.Delete(item);
                        }
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

            base.OnRemovedFromZone(zone);
        }
    }
}
