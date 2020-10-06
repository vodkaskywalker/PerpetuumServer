using Perpetuum.Containers;
using Perpetuum.Modules;
using Perpetuum.Modules.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Perpetuum.DataDumper.DataDumper;

namespace Perpetuum.DataDumper.Views {
    public class ModuleArmorPlateDataView : ModuleDataView {
        public double module_hp { get; set; }
        public double module_surface_hit { get; set; }
        public double module_demob_resist { get; set; }
    }
}
