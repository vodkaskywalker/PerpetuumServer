using Perpetuum.DataDumper.Views;
using Perpetuum.ExportedTypes;
using Perpetuum.Items.Ammos;
using Perpetuum.Modules.Weapons;
using Perpetuum.Services.ExtensionService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Perpetuum.DataDumper.DataDumper;

namespace Perpetuum.DataDumper { 
    public partial class DataDumper {
        public ModuleWeaponDataView NewWeaponModuleDataView(WeaponModule module) {
            var newView = new ModuleWeaponDataView();
            InitItemView(newView, module);

            var dictionaryData = module.ToDictionary();

            dictionaryData["ammoCategoryFlags"] = (CategoryFlags)dictionaryData["ammoCategoryFlags"];

            // Now we are ready to map up the data
            newView.cpu = module.CpuUsage;
            newView.reactor = module.PowerGridUsage;
            newView.ammo_type = GetLocalizedName(((CategoryFlags)dictionaryData["ammoCategoryFlags"]).ToString()).Replace(";undefined", "");
            newView.slot_status = module.ED.AttributeFlags.ActiveModule ? "Active" : "Passive";
            newView.slot_flags = module.ModuleFlag.ToString();
            newView.ammo_capacity = module.AmmoCapacity;
            newView.module_tier = module.ED.GameTierString();

            newView.module_accumulator = module.CoreUsage;
            newView.module_cycle = module.CycleTime.TotalSeconds;
            newView.module_damage = module.DamageModifier.Value * 100;
            newView.module_falloff = module.Properties.Single(x => x.Field == AggregateField.falloff).Value * 10;
            newView.module_hit_dispersion = module.Accuracy.Value;
            newView.module_optimal_range = module.OptimalRange * 10;

            var rangeModifierProperty = module.GetBasePropertyModifier(AggregateField.module_missile_range_modifier);

            if (rangeModifierProperty.HasValue) {
                string directionSymbol = "+";

                if (rangeModifierProperty.Value < 0) {
                    directionSymbol = "-";
                }

                newView.missile_optimal_range = directionSymbol + (rangeModifierProperty.Value - 1) * 100 + "%";

            }

            foreach (var extension in module.ED.EnablerExtensions.Keys) {
                newView.module_extensions_required += GetLocalizedName(extensionReader.GetExtensionName(extension.id)) + "(" + extension.level + ");";
            }

            return newView;
        }
    }
}