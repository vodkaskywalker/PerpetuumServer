using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Terrains.Materials.Minerals;
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones;
using System.Collections.Generic;
using System.Diagnostics;
using System.Transactions;
using Perpetuum.Zones.Terrains;
using System.Drawing;
using Perpetuum.Modules.ModuleProperties;

namespace Perpetuum.Modules
{
    public class RemoteControlledDrillerModule : GathererModule
    {
        private readonly RareMaterialHandler _rareMaterialHandler;
        private readonly MaterialHelper _materialHelper;
        private readonly ItemProperty _miningAmountModifier;

        public RemoteControlledDrillerModule(RareMaterialHandler rareMaterialHandler, MaterialHelper materialHelper)
            : base(CategoryFlags.undefined, true)
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

            if (zone != null)
            {
                DoExtractMinerals(zone);
            }
        }

        protected override int CalculateEp(int materialType)
        {
            return 0;
        }

        private void DoExtractMinerals(IZone zone)
        {
            var terrainLock = GetLock().ThrowIfNotType<TerrainLock>(ErrorCodes.InvalidLockType);
            var materialType = zone.Terrain.GetMaterialTypeAtPosition(terrainLock.Location);
            var materialInfo = _materialHelper.GetMaterialInfo(materialType);

            CheckEnablerEffect(materialInfo);

            var mineralLayer = zone.Terrain.GetMineralLayerOrThrow(materialInfo.Type);
            var materialAmount = materialInfo.Amount * _miningAmountModifier.Value;
            var extractedMaterials = Extract(mineralLayer, terrainLock.Location, (uint)materialAmount);

            extractedMaterials.Count.ThrowIfEqual(0, ErrorCodes.NoMineralOnTile);
            extractedMaterials.AddRange(_rareMaterialHandler.GenerateRareMaterials(materialInfo.EntityDefault.Definition));

            CreateBeam(terrainLock.Location, BeamState.AlignToTerrain);

            using (var scope = Db.CreateTransaction())
            {
                Debug.Assert(ParentRobot != null, "ParentRobot != null");

                var container = ParentRobot.GetContainer();

                Debug.Assert(container != null, "container != null");
                container.EnlistTransaction();

                foreach (var material in extractedMaterials)
                {
                    var item = (Item)Factory.CreateWithRandomEID(material.Definition);

                    item.Owner = Owner;
                    item.Quantity = material.Quantity;
                    container.AddItem(item, true);
                }

                //save container
                container.Save();
                Transaction.Current.OnCommited(() => container.SendUpdateToOwnerAsync());
                scope.Complete();
            }
        }

        private void CheckEnablerEffect(MaterialInfo materialInfo)
        {
            if (!Zone.Configuration.Terraformable)
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
