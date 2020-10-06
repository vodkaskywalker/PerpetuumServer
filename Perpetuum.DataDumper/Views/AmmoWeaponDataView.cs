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

    }
}
