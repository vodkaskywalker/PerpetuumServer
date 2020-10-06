using Perpetuum.DataDumper.Views;
using Perpetuum.ExportedTypes;
using Perpetuum.Items.Ammos;
using Perpetuum.Modules;
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
        private void FillModuleData(ModuleArmorHardenerDataView view, Module module) {
            view.module_resist_active_chemical = module.GetBasePropertyModifier(AggregateField.effect_resist_chemical).Value;
            view.module_resist_passive_chemical = module.GetBasePropertyModifier(AggregateField.resist_chemical).Value;
            view.module_resist_active_kinetic = module.GetBasePropertyModifier(AggregateField.effect_resist_kinetic).Value;
            view.module_resist_passive_kinetic = module.GetBasePropertyModifier(AggregateField.resist_kinetic).Value;
            view.module_resist_active_seismic = module.GetBasePropertyModifier(AggregateField.effect_resist_explosive).Value;
            view.module_resist_passive_seismic = module.GetBasePropertyModifier(AggregateField.resist_explosive).Value;
            view.module_resist_active_thermal = module.GetBasePropertyModifier(AggregateField.effect_resist_thermal).Value;
            view.module_resist_passive_thermal = module.GetBasePropertyModifier(AggregateField.resist_thermal).Value;

            var passives = new List<Tuple<double, double, string>> {
                                            new Tuple<double, double, string>(view.module_resist_passive_chemical,view.module_resist_active_chemical, "Chemical"),
                                            new Tuple<double, double, string>(view.module_resist_passive_kinetic, view.module_resist_active_kinetic, "Kinetic"),
                                            new Tuple<double, double, string>(view.module_resist_passive_seismic, view.module_resist_active_seismic, "Seismic"),
                                            new Tuple<double, double, string>(view.module_resist_passive_thermal, view.module_resist_active_thermal, "Thermal")
                                        };

            if (passives.Where(x => x.Item1 != 0).Count() > 1) {
                view.module_resist_type = "All";
                view.module_resist_passive = module.GetBasePropertyModifier(AggregateField.resist_chemical).Value;
            } else {
                var activeType = passives.Where(x => x.Item1 != 0).Single();
                view.module_resist_type = activeType.Item3;
                view.module_resist_passive = activeType.Item1;
                view.module_resist_active = activeType.Item2;
            }
        }
        public ModuleArmorHardenerDataView NewModuleArmorHardenerDataView(ActiveModule module) {
            var newView = new ModuleArmorHardenerDataView();
            InitActiveModuleView(newView, module);

            FillModuleData(newView, module);

            return newView;
        }

        public ModuleArmorHardenerDataView NewModuleArmorHardenerDataView(Module module) {
            var newView = new ModuleArmorHardenerDataView();
            InitModuleView(newView, module);

            FillModuleData(newView, module);

            return newView;
        }
    }
}