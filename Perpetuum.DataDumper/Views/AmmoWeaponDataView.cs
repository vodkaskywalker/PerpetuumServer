using Perpetuum.ExportedTypes;
using Perpetuum.Items.Ammos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Perpetuum.DataDumper.DataDumper;

namespace Perpetuum.DataDumper.Views {
    public class AmmoWeaponDataView : ItemDataView {
        public double? damage_chemical { get; set; }
        public double? damage_kinetic { get; set; }
        public double? damage_seismic { get; set; }
        public double? damage_thermal { get; set; }
        public double? damage_toxic { get; set; }
        public double damage_total { get => (damage_chemical ?? 0) + (damage_kinetic ?? 0) + (damage_seismic ?? 0) + (damage_thermal ?? 0); }
        public double? optimal_range { get; set; }
        public double? explosion_radius { get; set; }
        public string modifier_range { get; set; }
        public string modifier_falloff { get; set; }

        public AmmoWeaponDataView(Ammo item, DataDumper dumper) {
            dumper.InitItemView(this, item);

            var dictionaryData = item.ToDictionary();

            damage_chemical = item.GetBasePropertyModifier(AggregateField.damage_chemical).Value;
            damage_kinetic = item.GetBasePropertyModifier(AggregateField.damage_kinetic).Value;
            damage_seismic = item.GetBasePropertyModifier(AggregateField.damage_explosive).Value;
            damage_thermal = item.GetBasePropertyModifier(AggregateField.damage_thermal).Value;
            damage_toxic = item.GetBasePropertyModifier(AggregateField.damage_toxic).Value;

            modifier_falloff = (item.GetBasePropertyModifier(AggregateField.falloff).Value * 10).ToString();
            explosion_radius = item.GetBasePropertyModifier(AggregateField.explosion_radius).Value;

            // Empty out the 0's
            if (damage_chemical == 0) { damage_chemical = null; };
            if (damage_kinetic == 0) { damage_kinetic = null; };
            if (damage_seismic == 0) { damage_seismic = null; };
            if (damage_thermal == 0) { damage_thermal = null; };
            if (damage_toxic == 0) { damage_toxic = null; };

            if (modifier_falloff == "0") { modifier_falloff = null; }; // keeping this as a string for now because other modules may use it as a modifier?
            if (explosion_radius == 0) { explosion_radius = null; };

            // This will get the property even if it doesn't explicitly exist
            // It will be set to the default value, but the HasValue will tell
            // us if it's something other than the default

            modifier_range = GetModifierString(item.GetBasePropertyModifier(AggregateField.optimal_range_modifier));

            var rangeProp = item.GetBasePropertyModifier(AggregateField.optimal_range);

            if (rangeProp.HasValue) {
                optimal_range = rangeProp.Value * 10;
            }
        }

    }


}
