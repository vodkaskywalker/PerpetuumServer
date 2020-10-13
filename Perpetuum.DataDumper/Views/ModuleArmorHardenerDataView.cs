using Perpetuum.Containers;
using Perpetuum.ExportedTypes;
using Perpetuum.Modules;
using Perpetuum.Modules.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Perpetuum.DataDumper.DataDumper;

namespace Perpetuum.DataDumper.Views {
    public class ModuleArmorHardenerDataView : ActiveModuleDataView {

        public double module_resist_active_chemical { get; set; }
        public double module_resist_passive_chemical { get; set; }
        public double module_resist_active_kinetic { get; set; }
        public double module_resist_passive_kinetic { get; set; }
        public double module_resist_active_seismic { get; set; }
        public double module_resist_passive_seismic { get; set; }
        public double module_resist_active_thermal { get; set; }
        public double module_resist_passive_thermal { get; set; }
        public string module_resist_type { get; set; }
        public double module_resist_passive { get; set; }
        public double module_resist_active { get; set; }

        public ModuleArmorHardenerDataView(ActiveModule item, DataDumper dumper) {
            dumper.InitActiveModuleView(this, item);

            FillModuleData(item);
        }

        public ModuleArmorHardenerDataView(Module item, DataDumper dumper) {
            dumper.InitModuleView(this, item);

            FillModuleData(item);
        }

        private void FillModuleData(Module module) {
            module_resist_active_chemical = module.GetBasePropertyModifier(AggregateField.effect_resist_chemical).Value;
            module_resist_passive_chemical = module.GetBasePropertyModifier(AggregateField.resist_chemical).Value;
            module_resist_active_kinetic = module.GetBasePropertyModifier(AggregateField.effect_resist_kinetic).Value;
            module_resist_passive_kinetic = module.GetBasePropertyModifier(AggregateField.resist_kinetic).Value;
            module_resist_active_seismic = module.GetBasePropertyModifier(AggregateField.effect_resist_explosive).Value;
            module_resist_passive_seismic = module.GetBasePropertyModifier(AggregateField.resist_explosive).Value;
            module_resist_active_thermal = module.GetBasePropertyModifier(AggregateField.effect_resist_thermal).Value;
            module_resist_passive_thermal = module.GetBasePropertyModifier(AggregateField.resist_thermal).Value;

            var passives = new List<Tuple<double, double, string>> {
                                            new Tuple<double, double, string>(module_resist_passive_chemical,module_resist_active_chemical, "Chemical"),
                                            new Tuple<double, double, string>(module_resist_passive_kinetic, module_resist_active_kinetic, "Kinetic"),
                                            new Tuple<double, double, string>(module_resist_passive_seismic, module_resist_active_seismic, "Seismic"),
                                            new Tuple<double, double, string>(module_resist_passive_thermal, module_resist_active_thermal, "Thermal")
                                        };

            if (passives.Where(x => x.Item1 != 0).Count() > 1) {
                module_resist_type = "All";
                module_resist_passive = module.GetBasePropertyModifier(AggregateField.resist_chemical).Value;
            } else {
                var activeType = passives.Where(x => x.Item1 != 0).Single();
                module_resist_type = activeType.Item3;
                module_resist_passive = activeType.Item1;
                module_resist_active = activeType.Item2;
            }
        }
    }

}
