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
    public class ModuleArmorRepairerDataView : ItemDataView {
        public string module_tier { get; set; }
        public double cpu { get; set; }
        public double reactor { get; set; }
        
        public double module_accumulator { get; set; }
        public double module_cycle { get; set; }
        public double module_repair_amount { get; set; }

        public string module_extensions_required { get; set; }
    }
}
