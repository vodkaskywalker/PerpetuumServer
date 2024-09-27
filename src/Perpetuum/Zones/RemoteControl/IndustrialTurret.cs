﻿using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Services.Looting;
using Perpetuum.Services.Standing;
using Perpetuum.Units;
using Perpetuum.Zones.NpcSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Transactions;

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
            using (TransactionScope scope = Db.CreateTransaction())
            {
                try
                {
                    Robots.RobotInventory robotInventory = GetContainer();

                    Debug.Assert(robotInventory != null);

                    List<LootItem> lootItems = new List<LootItem>();

                    foreach (Items.Item item in robotInventory
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
                        _ = LootContainer.Create()
                            .AddLoot(lootItems)
                            .BuildAndAddToZone(zone, CurrentPosition);
                    }

                    Save();

                    scope.Complete();
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }

        protected override void OnUpdate(TimeSpan time)
        {
            if (!IsInOperationalRange)
            {
                Kill();
            }

            base.OnUpdate(time);
        }
    }
}