using Perpetuum.DataDumper.Views;
using Perpetuum.ExportedTypes;
using Perpetuum.Items.Ammos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Perpetuum.DataDumper.DataDumper;

namespace Perpetuum.DataDumper { 
    public partial class DataDumper {
        public AmmoWeaponDataView NewAmmoWeaponDataView(Ammo item) {
            var newView = new AmmoWeaponDataView();
            InitItemView(newView, item);

            var dictionaryData = item.ToDictionary();

            newView.damage_chemical = item.GetBasePropertyModifier(AggregateField.damage_chemical).Value;
            newView.damage_kinetic = item.GetBasePropertyModifier(AggregateField.damage_kinetic).Value;
            newView.damage_seismic = item.GetBasePropertyModifier(AggregateField.damage_explosive).Value;
            newView.damage_thermal = item.GetBasePropertyModifier(AggregateField.damage_thermal).Value;
            newView.damage_toxic = item.GetBasePropertyModifier(AggregateField.damage_toxic).Value;

            newView.modifier_falloff = (item.GetBasePropertyModifier(AggregateField.falloff).Value * 10).ToString();
            newView.explosion_radius = item.GetBasePropertyModifier(AggregateField.explosion_radius).Value;

            // Empty out the 0's
            if (newView.damage_chemical == 0) { newView.damage_chemical = null; };
            if (newView.damage_kinetic == 0) { newView.damage_kinetic = null; };
            if (newView.damage_seismic == 0) { newView.damage_seismic = null; };
            if (newView.damage_thermal == 0) { newView.damage_thermal = null; };
            if (newView.damage_toxic == 0) { newView.damage_toxic = null; };

            if (newView.modifier_falloff == "0") { newView.modifier_falloff = null; }; // keeping this as a string for now because other modules may use it as a modifier?
            if (newView.explosion_radius == 0) { newView.explosion_radius = null; };

            // This will get the property even if it doesn't explicitly exist
            // It will be set to the default value, but the HasValue will tell
            // us if it's something other than the default

            newView.modifier_range = GetModifierString(item.GetBasePropertyModifier(AggregateField.optimal_range_modifier));

            var rangeProp = item.GetBasePropertyModifier(AggregateField.optimal_range);

            if (rangeProp.HasValue) {
                newView.optimal_range = rangeProp.Value * 10;
            }

            return newView;
        }
    }
}