using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Players;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.RemoteControl;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones.Terrains.Materials.Minerals;

namespace Perpetuum.Modules
{
    public class DrillerModule : GathererModule
    {
        private const int MAX_EP_PER_DAY = 1440;
        private readonly RareMaterialHandler _rareMaterialHandler;
        private readonly MaterialHelper _materialHelper;
        private readonly ItemProperty _miningAmountModifier;
        private readonly ISet<int> LIQUIDS = new HashSet<int>(new int[] 
        {
            (int)MaterialType.Crude,
            (int)MaterialType.Liquizit,
            (int)MaterialType.Epriton
        });

        public DrillerModule(CategoryFlags ammoCategoryFlags,RareMaterialHandler rareMaterialHandler,MaterialHelper materialHelper)
            : base(ammoCategoryFlags, true)
        {
            _rareMaterialHandler = rareMaterialHandler;
            _materialHelper = materialHelper;
            _miningAmountModifier = new MiningAmountModifierProperty(this);
            AddProperty(_miningAmountModifier);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public override void UpdateProperty(AggregateField field)
        {
            switch (field)
            {
                case AggregateField.mining_amount_modifier:
                case AggregateField.effect_mining_amount_modifier:
                    {
                        _miningAmountModifier.Update();

                        return;
                    }
            }

            base.UpdateProperty(field);
        }

        public List<ItemInfo> Extract(MineralLayer layer, Point location, uint amount)
        {
            if (!layer.HasMineral(location))
            {
                return new List<ItemInfo>();
            }

            var extractor = new MineralExtractor(location, amount, _materialHelper);

            layer.AcceptVisitor(extractor);

            return new List<ItemInfo>(extractor.Items);
        }

        protected override void OnAction()
        {
            var zone = Zone;

            if ( zone != null)
            {
                DoExtractMinerals(zone);
            }

            ConsumeAmmo();
        }

        protected override int CalculateEp(int materialType)
        {
            var activeGathererModules = this is RemoteControlledDrillerModule
                ? ParentRobot.ActiveModules.OfType<RemoteControlledDrillerModule>().Where(m => m.State.Type != ModuleStateType.Idle).ToArray()
                : ParentRobot.ActiveModules.OfType<DrillerModule>().Where(m => m.State.Type != ModuleStateType.Idle).ToArray();

            if (activeGathererModules.Length == 0)
            {
                return 0;
            }

            var avgCycleTime = activeGathererModules.Select(m => m.CycleTime).Average();
            var t = TimeSpan.FromDays(1).Divide(avgCycleTime);
            var chance = (double)MAX_EP_PER_DAY / t.Ticks;

            chance /= activeGathererModules.Length;

            if (LIQUIDS.Contains(materialType))
            {
                chance /= 2.0;
            }

            var rand = FastRandom.NextDouble();

            if (rand <= chance)
            {
                return 1;
            }

            return 0;
        }

        public void DoExtractMinerals(IZone zone)
        {
            var terrainLock = GetLock().ThrowIfNotType<TerrainLock>(ErrorCodes.InvalidLockType);

            MaterialType materialType;

            if (ParentRobot is RemoteControlledTurret)
            {
                materialType = zone.Terrain.GetMaterialTypeAtPosition(terrainLock.Location);
            }
            else
            {
                MiningAmmo ammo = GetAmmo() as MiningAmmo;

                if (ammo == null)
                {
                    return;
                }

                materialType = ammo.MaterialType;
            }

            var materialInfo = _materialHelper.GetMaterialInfo(materialType);

            CheckEnablerEffect(materialInfo);

            var mineralLayer = zone.Terrain.GetMineralLayerOrThrow(materialInfo.Type);
            var materialAmount = materialInfo.Amount * _miningAmountModifier.Value;
            var extractedMaterials = Extract(mineralLayer, terrainLock.Location,(uint) materialAmount);

            extractedMaterials.Count.ThrowIfEqual(0, ErrorCodes.NoMineralOnTile);
            extractedMaterials.AddRange(_rareMaterialHandler.GenerateRareMaterials(materialInfo.EntityDefault.Definition));

            CreateBeam(terrainLock.Location, BeamState.AlignToTerrain);

            using (var scope = Db.CreateTransaction())
            {
                Debug.Assert(ParentRobot != null, "ParentRobot != null");

                var container = ParentRobot.GetContainer();

                Debug.Assert(container != null, "container != null");
                container.EnlistTransaction();

                var player = ParentRobot is RemoteControlledTurret
                    ? (ParentRobot as RemoteControlledTurret).Player
                    : ParentRobot as Player;

                Debug.Assert(player != null,"player != null");

                foreach (var material in extractedMaterials)
                {
                    var item = (Item)Factory.CreateWithRandomEID(material.Definition);

                    item.Owner = Owner;
                    item.Quantity = material.Quantity;
                    container.AddItem(item, true);

                    var drilledMineralDefinition = material.Definition;
                    var drilledQuantity = material.Quantity;

                    player.MissionHandler.EnqueueMissionEventInfo(new DrillMineralEventInfo(player,drilledMineralDefinition,drilledQuantity,terrainLock.Location));
                    player.Zone?.MiningLogHandler.EnqueueMiningLog(drilledMineralDefinition,drilledQuantity);
                }

                //save container
                container.Save();
                OnGathererMaterial(zone, player, (int) materialInfo.Type);
                Transaction.Current.OnCommited(() => container.SendUpdateToOwnerAsync());
                scope.Complete();
            }
        }

        private void CheckEnablerEffect(MaterialInfo materialInfo)
        {
            if ( !Zone.Configuration.Terraformable )
            {
                return;
            }

            if (!materialInfo.EnablerExtensionRequired)
            {
                return;
            }

            var containsEnablerEffect = ParentRobot.EffectHandler.ContainsEffect(EffectCategory.effcat_pbs_mining_tower_effect);

            containsEnablerEffect.ThrowIfFalse(ErrorCodes.MiningEnablerEffectRequired);
        }
    }
}