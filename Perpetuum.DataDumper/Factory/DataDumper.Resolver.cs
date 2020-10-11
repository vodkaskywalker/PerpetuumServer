using Open.Nat;
using Perpetuum.DataDumper.Views;
using Perpetuum.EntityFramework;
using Perpetuum.Items.Ammos;
using Perpetuum.Modules;
using Perpetuum.Modules.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.DataDumper {
    public partial class DataDumper {
        public dynamic DumpDataView(DataExportMapping mapping) {
            Type[] typeArgs = new Type[] { typeof(string) };
            var reflectedMethod = typeof(DataDumper).GetMethod("DumpDataView", typeArgs);
            var genericMethod = reflectedMethod.MakeGenericMethod(mapping.ViewType);
            dynamic results = genericMethod.Invoke(this, new object[] { mapping.Category });

            // No need, just use dynamic
            //var listType = typeof(List<>).MakeGenericType(mapping.ViewType);

            //var typedResults = Convert.ChangeType(results, listType);
            return results;
        }

        public List<T> DumpDataView<T>(string categoryName) where T : EntityDataView {
            var returnItems = new List<T>();

            // Handle sparks first, refactor later
            if (typeof(T) == typeof(SparkDataView)) {
                var sparkData = sparkRepository.GetAll();

                foreach (var spark in sparkData) {
                    try {
                        var currentView = NewSparkDataView(spark);

                        returnItems.Add(currentView as T);
                    } catch (Exception ex) {
                        Console.WriteLine(ex.Message);
                    }
                }

                return returnItems;

            }

            var categoryData = GetDataByItemCategoryName(categoryName);

            foreach (var categoryItem in categoryData) {
                T currentView;

                string itemName = categoryItem["definitionname"].ToString();
                int itemId = (int)categoryItem["definition"];

                try {
                    object currentObject = entityFactory.Create(itemName, EntityIDGenerator.Random);

                    
                    if (typeof(T) == typeof(ModuleWeaponDataView)) {
                        currentView = NewWeaponModuleDataView(currentObject as WeaponModule) as T;
                    } else if (typeof(T) == typeof(AmmoWeaponDataView)) {
                        currentView = NewAmmoWeaponDataView(currentObject as Ammo) as T;
                    } else if (typeof(T) == typeof(ModuleArmorRepairerDataView)) {
                        currentView = NewModuleArmorRepairDataView(currentObject as ArmorRepairModule) as T;
                    } else if (typeof(T) == typeof(ModuleArmorPlateDataView)) {
                        currentView = NewModuleArmorPlateDataView(currentObject as Module) as T;
                    } else if (typeof(T) == typeof(ModuleRemoteArmorRepairerDataView)) {
                        currentView = NewModuleRemoteArmorRepairerDataView(currentObject as ActiveModule) as T;
                    } else if (typeof(T) == typeof(ModuleERPDataView)) {
                        currentView = NewModuleERPDataView(currentObject as Module) as T;
                    } else if (typeof(T) == typeof(ModuleShieldGeneratorDataView)) {
                        currentView = NewModuleShieldGeneratorDataView(currentObject as ActiveModule) as T;
                    } else if (typeof(T) == typeof(ModuleArmorHardenerDataView)) {
                        if (currentObject.GetType() == typeof(Perpetuum.Modules.EffectModules.ArmorHardenerModule)) {
                            currentView = NewModuleArmorHardenerDataView(currentObject as ActiveModule) as T;
                        } else {
                            currentView = NewModuleArmorHardenerDataView(currentObject as Module) as T;
                        }
                    } else if (typeof(T) == typeof(ModuleDrillerDataView)) {
                        currentView = NewModuleDrillerDataView(currentObject as DrillerModule) as T;
                    } else {
                        continue;
                    }

                    returnItems.Add(currentView);
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }

            }
            return returnItems;
        }
    }
}
