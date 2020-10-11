using Autofac;
using Perpetuum.Bootstrapper;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Modules.Weapons;
using Perpetuum.Robots;
using Perpetuum.Services.ExtensionService;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Perpetuum.Items.Ammos;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.DataDumper.Views;
using System.Windows.Forms;
using System.CodeDom;
using Perpetuum.Services.Sparks;
using Perpetuum.Groups.Alliances;
using System.Reflection;
using NPOI.SS.UserModel;

namespace Perpetuum.DataDumper
{
    public partial class DataDumper
    {
        private string serverRoot;
        private string dictionaryPath;

        private IContainer container;
        EntityFactory entityFactory;
        IEntityServices entityServices;
        IExtensionReader extensionReader;
        IEntityDefaultReader defaultReader;
        Dictionary<string, string> itemNames;
        ISparkRepository sparkRepository;

        // Some static lists for helpers
        public static SlotFlags[] sizeFlags = new SlotFlags[] { SlotFlags.small, SlotFlags.medium, SlotFlags.large };
        public static SlotFlags[] typeFlags = new SlotFlags[] { SlotFlags.turret, SlotFlags.missile, SlotFlags.melee, SlotFlags.industrial, SlotFlags.ew_and_engineering };
        public static SlotFlags[] locationFlags = new SlotFlags[] { SlotFlags.head, SlotFlags.chassis, SlotFlags.leg };
        public static List<CategoryFlags> ammoWeaponCategories = new List<CategoryFlags> {
                                                                CategoryFlags.cf_railgun_ammo,
                                                                CategoryFlags.cf_laser_ammo,
                                                                CategoryFlags.cf_missile_ammo,
                                                                CategoryFlags.cf_projectile_ammo };

        private static List<DataDumpConfiguration> views = new List<DataDumpConfiguration> {
            new DataDumpConfiguration { ViewType = typeof(AmmoWeaponDataView), ViewCategory = "cf_ammo" },
            new DataDumpConfiguration { ViewType = typeof(ModuleWeaponDataView), ViewCategory = "cf_weapons" }
            
        };

        public DataDumper(IContainer container, string serverRoot, string dictionaryPath)
        {
            this.serverRoot = serverRoot;

            this.dictionaryPath = dictionaryPath;

            this.container = container;

            entityFactory = container.Resolve<EntityFactory>(); 

            extensionReader = container.Resolve<IExtensionReader>();

            defaultReader = container.Resolve<IEntityDefaultReader>();

            var productionDataReader = container.Resolve<IProductionDataAccess>();

            sparkRepository = container.Resolve<ISparkRepository>();

            entityServices = container.Resolve<IEntityServices>();

            // var getEd = defaultReader.GetByName("def_named1_small_armor_repairer");
            // var getCats = String.Join(";", getEd.CategoryFlags.GetCategoryFlagsTree().Where(x => x.ToString() != "undefined").Select(x => x.ToString()).ToList());
            // var testMissile = productionDataReader.ProductionComponents[64];
            // var testRobot = productionDataReader.ProductionComponents[193]; 
            // var testRobot2 = productionDataReader.ProductionComponents[208];

            var itemNames = new Dictionary<string, string>();

            var dataLines = System.IO.File.ReadAllText(dictionaryPath);

            var dictionaryText = GenXY.GenxyConverter.Deserialize(dataLines);

            if (dictionaryText.ContainsKey("dictionary")) {
                var sourceDict = (Dictionary<string, object>)dictionaryText["dictionary"];

                foreach (var item in sourceDict) {
                    itemNames.Add(item.Key, item.Value.ToString());
                }

            } else {
                throw new Exception("Dictionary file is invalid");
            }

            //foreach (var line in dataLines)
            //{
            //    var parts = line.Split(new char[] { '\t' });
            //    itemNames.Add(parts[0], parts[1]);
            //}

            // Now read the names from JSON files
            string dictionaryLocation = System.IO.Path.Combine(serverRoot, @"customDictionary\0.json");

            var jsonData = JsonConvert.DeserializeObject<IDictionary<string, string>>(System.IO.File.ReadAllText(dictionaryLocation));

            foreach (var item in jsonData) {
                if (itemNames.ContainsKey(item.Key)) {
                    itemNames[item.Key] = item.Value;
                } else {
                    itemNames.Add(item.Key, item.Value);
                }
            }

            Console.WriteLine($"{dataLines.Length} dictionary names loaded");
        }

        public string GetLocalizedName(string itemKey)
        {
            if (itemNames.ContainsKey(itemKey))
            {
                return itemNames[itemKey];
            } else
            {
                return itemKey;
            }
        }

        public class EntityDataView {
            public string item_name { get; set; } // This should actually be renamed...
            public string item_key { get; set; }
            public List<string> item_categories { get; set; }
        }

        public class ItemDataView : EntityDataView {
            public double item_mass { get; set; }
            public double item_volume { get; set; }
            public double item_volume_packed { get; set; }

        }

        public class ModuleDataView : ItemDataView {
            public string module_tier { get; set; }
            public double module_cpu { get; set; }
            public double module_reactor { get; set; }
            public List<string> module_extensions_required { get; set; }
        }

        public class ActiveModuleDataView : ModuleDataView {
            // These are nullable because some items may be
            // in a group of active modules but are themselves
            // passive and we don't want to show 0 for them
            public double? module_accumulator { get; set; }
            public double? module_cycle { get; set; }

        }

        public static string GenerateCargoDefinition(Type viewType, string tableName, string listDelimiter = ";") {
            string header = "<noinclude>\n{{#cargo_declare:_table="+tableName+"\n";
            string body = "";
            string footer = "}}\n</noinclude>";
            var allProperties = viewType.GetProperties().OrderBy(x=> x.Name).ToList();

            foreach (var property in allProperties) {
                var cargoAttribute = property.GetCustomAttribute<CargoTypeAttribute>();

                string cargoType = "String"; // <- This is the default in Cargo

                if (cargoAttribute != null) {
                    cargoType = cargoAttribute.Type;
                } else {
                    // Let's set some defaults by property type
                    if (property.PropertyType == typeof(double)) {
                        cargoType = "Float";
                    } else if (property.PropertyType == typeof(int)) {
                        cargoType = "Integer";
                    } else if (property.PropertyType == typeof(bool)) {
                        cargoType = "Boolean";
                    } else if(property.PropertyType == typeof(List<string>)) {
                        cargoType = $"List ({listDelimiter}) of String";
                    } else if (property.PropertyType == typeof(List<int>)) {
                        cargoType = $"List ({listDelimiter}) of Integer";
                    } else if (property.PropertyType == typeof(List<double>)) {
                        cargoType = $"List ({listDelimiter}) of Float";
                    }
                }

                body += $"|{property.Name}={cargoType}\n";
            }

            return header + body + footer;

        }

        private void InitItemView(ItemDataView view, Entity entity) {
            view.item_name = GetLocalizedName(entity.ED.Name);
            view.item_key = entity.ED.Name;
            view.item_categories = entity.ED.CategoryFlags.GetCategoryFlagsTree().Where(x=> x.ToString() != "undefined").Select(x => x.ToString()).ToList();
            view.item_mass = entity.Mass;
            view.item_volume_packed = entity.ED.CalculateVolume(true, 1);
            view.item_volume = entity.ED.CalculateVolume(false, 1);
        }

        private void InitModuleView(ModuleDataView view, Modules.Module module) {
            InitItemView(view, module);

            view.module_tier = module.ED.GameTierString();
            view.module_cpu = module.CpuUsage;
            view.module_reactor = module.PowerGridUsage;

            view.module_extensions_required = new List<string>();

            foreach (var extension in module.ED.EnablerExtensions.Keys) {
                view.module_extensions_required.Add(GetLocalizedName(extensionReader.GetExtensionName(extension.id)) + "(" + extension.level + ")");
            }
        }

        private void InitActiveModuleView(ActiveModuleDataView view, ActiveModule module) {
            InitModuleView(view, module);

            view.module_accumulator = module.CoreUsage;
            view.module_cycle = module.CycleTime.TotalSeconds;
        }

        private string GetModifierString(ItemPropertyModifier mod) {
            var returnValue = "";
            if (mod.HasValue) {
                returnValue = ((mod.Value - 1) * 100) + "%";

                if (mod.Value - 1 > 0) {
                    returnValue = "+" + returnValue;
                }
            }

            return returnValue;
        }

        // Factory Methods
        
        private class DataDumpConfiguration {
            public Type ViewType { get; set; }
            public String ViewCategory { get; set; }
        }
        

        public void DumpGenericData(string categoryName, string outputPath)
        {
            var returnData = new List<List<string>>();
            List<string> headers = null;
            var categoryData = GetDataByItemCategoryName(categoryName);

            foreach (var categoryItem in categoryData)
            {
                string itemName = categoryItem["definitionname"].ToString();
                int itemId = (int)categoryItem["definition"];
                try
                {
                    // var currentDefaults = defaultReader.GetByName(itemName);
                    var currentObject = entityFactory.Create(itemName, EntityIDGenerator.Random);

                    var dictionaryData = currentObject.ToDictionary();

                    if (currentObject is Modules.Weapons.WeaponModule)
                    {
                        dictionaryData["ammoCategoryFlags"] = (CategoryFlags)dictionaryData["ammoCategoryFlags"];
                    }

                    if (headers == null || headers.Count == 0)
                    {
                        // We only want certain properties, yo!
                        headers = currentObject.GetType().GetProperties().Select(i => i.Name).ToList(); // new List<string> { "BlobEmission", "BlobEmissionRadius", "Mass", "Volume", "MaxTargetingRange", "PowerGrid", "Cpu", "AmmoReloadTime", "MissileHitChance", "Height", "ArmorMax", "ActualMass", "CoreMax", "SignatureRadius", "SensorStrength", "DetectionStrength", "StealthStrength", "Massiveness", "ReactorRadiation", "Slope", "CoreRechargeTime", "BlockingRadius", "HitSize" };
                        returnData.Add((new string[] { "Item Name" }).Concat(headers).ToList());
                    }

                    var currentProperties = new List<string>();

                    currentProperties.Add(itemName);

                    // Parse the object
                    foreach (string prop in headers)
                    {
                        try
                        {
                            var currentProp = currentObject.GetType().GetProperty(prop);

                            if (currentProp == null)
                            {
                                currentProperties.Add("Error: Prop not found");
                            }
                            else
                            {
                                var currentMethod = currentProp.GetGetMethod(true);

                                if (currentMethod == null)
                                {
                                    currentProperties.Add("Error: Getter not found");
                                }
                                else
                                {
                                    object value = currentMethod.Invoke(currentObject, null);
                                    string stringValue = value?.ToString() ?? "";
                                    currentProperties.Add(stringValue.Replace(",", ";"));
                                }

                            }
                        }
                        catch (Exception)
                        {
                            currentProperties.Add("EXCEPTION");
                        }

                    }

                    returnData.Add(currentProperties);

                }
                catch (Exception)
                {
                    returnData.Add(new List<string> { itemName, "BIG EXCEPTION" });
                }
            }

            System.IO.File.WriteAllLines(outputPath, returnData.Select(x => String.Join(",", x)));

        }

        // cf_railguns, look at definition column

        public void WriteDataView(List<List<string>> dataRows, string filePath)
        {
            System.IO.File.WriteAllLines(filePath, dataRows.Select(x => String.Join(",", x)));
        }

        public void WriteDataView(List<List<string>> dataRows, string sheetName, ISheet worksheet, ref int currentDataRow) {
            // Deal with the header
            // If we are writing later than the first row in our sheet then we will skip
            // the first row in our data because it will contain the header
            int skipRow = 0;

            if (currentDataRow > 0) {
                skipRow = 1;
            }

            foreach (var dataRow in dataRows.Skip(skipRow).ToList()) {
                int currentColumn = 0;
                var currentRow = worksheet.CreateRow(currentDataRow);
                foreach (var dataValue in dataRow) {
                    currentRow.CreateCell(currentColumn).SetCellValue(dataValue);
                    currentColumn++;
                }
                currentDataRow++;
            }
        }

        /// <summary>
        /// This will write the properties as headers and values as rows
        /// </summary>
        public List<List<string>> ComposeDataView<T>(List<T> data, string wikiTableName = "wikitable") {
            var returnData = new List<List<string>>();

            if (data is null || data.Count == 0) {
                return returnData; // Do nothing
            }

            var headers = data.First().GetType().GetProperties().Select(i => i.Name).ToList();

            returnData.Add(headers.Concat(new List<string> { "wiki" }).ToList());

            foreach (var item in data)
            {
                var currentProperties = new List<string>();

                // Add the wiki markup at the end
                // {{#cargo_store:_table=WeaponStats|module_name=Niani medium EM-gun|module_key=def_artifact_a_longrange_medium_railgun|module_categories=cf_medium_railguns;cf_railguns;cf_weapons;cf_robot_equipment|cpu=45|reactor=205|ammo_type=Medium slugs|slot_status=Active|ammo_capacity=50|module_mass=650|module_volume_packed=0.25|module_tier=T3+|module_volume=0.5|module_accumulator=32|module_cycle=10|module_damage=250|module_falloff=60|module_hit_dispersion=14|module_optimal_range=300|module_extensions_required=Advanced magnetostatics(2);|slot_type=turret|slot_size=medium|slot_location=chassis}}
                string wikiData = $"{{{{#cargo_store:_table={wikiTableName}";

                foreach (string prop in headers)
                {
                    var currentProp = item.GetType().GetProperty(prop);
                    var currentValue = "";

                    try
                    {
                        if (currentProp == null)
                        {
                            currentValue = "Error: Prop not found";
                        }
                        else
                        {
                            if (currentProp.PropertyType.Name.Contains("List")) {
                                currentValue = String.Join(";", (currentProp.GetValue(item) as IEnumerable<object>));
                            } else {
                                currentValue = currentProp.GetValue(item)?.ToString().Replace(",",";") ?? "";
                            }
                            //var currentMethod = currentProp.GetGetMethod(true);

                            //if (currentMethod == null)
                            //{
                            //    currentValue = "Error: Getter not found";
                            //}
                            //else
                            //{
                            //    object value = currentMethod.Invoke(item, null);
                            //    string stringValue = value?.ToString() ?? "";
                            //    currentValue = stringValue.Replace(",", ";");
                            //}

                        }
                    }
                    catch (Exception)
                    {
                        currentValue = "EXCEPTION";
                    }

                    currentProperties.Add(currentValue);

                    wikiData += $"|{prop}={currentValue}";

                }

                wikiData += "}}";

                currentProperties.Add(wikiData);

                returnData.Add(currentProperties);

            }

            return returnData;

        }

        

        private List<IDataRecord> GetDataByItemCategoryName(string categoryName)
        {
            var itemData = Db.Query().CommandText(
                    $@"DECLARE @lookupFlag bigint;
                    SET @lookupFlag = (select value from dbo.categoryFlags where name='{categoryName}');
                    SELECT * from entitydefaults where (categoryflags & CAST(dbo.GetCFMask(@lookupFlag)as BIGINT) = @lookupFlag)
                    AND enabled=1 AND hidden=0 AND purchasable=1;"
                ).Execute();

            return itemData;
        }

        // var Extension
        // var test2 = entityFactory.Create(684, EntityIDGenerator.Random, true);
        // var test3 = entityFactory.Create("def_standard_small_armor_plate", EntityIDGenerator.Random, true);

        public void GetRobotData(string outputPath)
        {
            var botNames = GetDataByItemCategoryName("cf_robots");

            var returnData = new List<List<string>>();
            List<string> headers = null;

            var botDataList = new List<Entity>();

            foreach (var botData in botNames)
            {
                string botName = botData["definitionname"].ToString();
                try
                {
                    var currentBot = (Robot)entityFactory.Create(botName, EntityIDGenerator.Random);

                    currentBot.Unpack();

                    botDataList.Add(currentBot);

                    // var dataView = new RobotDataView(currentBot as Robot);

                    if (headers == null || headers.Count == 0)
                    {
                        // We only want certain properties, yo!
                        headers = new List<string> { "BlobEmission", "BlobEmissionRadius", "Mass", "Volume", "MaxTargetingRange", "PowerGrid", "Cpu", "AmmoReloadTime", "MissileHitChance", "Height", "ArmorMax", "ActualMass", "CoreMax", "SignatureRadius", "SensorStrength", "DetectionStrength", "StealthStrength", "Massiveness", "ReactorRadiation", "Slope", "CoreRechargeTime", "BlockingRadius", "HitSize" }; // currentBot.GetType().GetProperties().Select(i => i.Name).ToList();

                        var propNames = ((Robot)currentBot).Properties.Select(x => x.Field.ToString());
                        var slotProps = new List<string> { "Capacity", "SlotsHead", "SlotsChassis", "SlotsLegs" };
                        // headers.Remove("Definition");
                        returnData.Add((new string[] { "Bot Name" }).Concat(headers).Concat(propNames).Concat(slotProps).ToList());
                    }

                    var currentProperties = new List<string>();

                    currentProperties.Add(botName);

                    // Parse the object
                    foreach (string prop in headers)
                    {
                        try
                        {
                            string errorMessage;
                            var currentProp = currentBot.GetType().GetProperty(prop);

                            if (currentProp == null)
                            {
                                currentProperties.Add("Error: Prop not found");
                            }
                            else
                            {
                                var currentMethod = currentProp.GetGetMethod(true);

                                if (currentMethod == null)
                                {
                                    currentProperties.Add("Error: Getter not found");
                                }
                                else
                                {
                                    object value = currentMethod.Invoke(currentBot, null);
                                    string stringValue = value?.ToString() ?? "";
                                    currentProperties.Add(stringValue.Replace(",", ";"));
                                }

                            }
                        }
                        catch (Exception)
                        {
                            currentProperties.Add("EXCEPTION");
                        }

                    }

                    Robot input = (Robot)currentBot;

                    // Parse the extra properties
                    foreach (var item in input.Properties)
                    {
                        currentProperties.Add(item.Value.ToString());
                    }

                    RobotHead head = input.GetRobotComponent<RobotHead>();
                    RobotChassis chassis = input.GetRobotComponent<RobotChassis>();
                    RobotLeg legs = input.GetRobotComponent<RobotLeg>();
                    RobotInventory inventory = input.Components.OfType<RobotInventory>().SingleOrDefault();

                    currentProperties.Add(inventory.GetCapacityInfo()["capacity"].ToString());
                    currentProperties.Add(String.Join(";", head.ED.Options.SlotFlags.Select(x => ((SlotFlags)x).ToString())));
                    currentProperties.Add(String.Join(";", chassis.ED.Options.SlotFlags.Select(x => ((SlotFlags)x).ToString().Replace(", ", "|"))));
                    currentProperties.Add(String.Join(";", legs.ED.Options.SlotFlags.Select(x => ((SlotFlags)x).ToString().Replace(", ", "|"))));



                    // This returns the ID and required level
                    var extData = input.ExtensionBonusEnablerExtensions.ToList();

                    for (int i = 1; i <= 10; i++)
                    {
                        if (extData.Count >= i)
                        {
                            var currentItem = extData[i - 1];
                            currentProperties.Add(extensionReader.GetExtensionName(currentItem.id) + "=" + currentItem.level);
                        }
                        else
                        {
                            currentProperties.Add(""); // Fill in with empty
                        }
                    }

                    var requiredExtensions = input.ED.EnablerExtensions; // Gets the list of extensiosn required from definition. seems to match above.

                    for (int i = 1; i <= 10; i++)
                    {
                        var currentKeys = requiredExtensions.Keys.ToList();
                        if (currentKeys.Count >= i)
                        {
                            var currentItem = currentKeys[i - 1];
                            currentProperties.Add(extensionReader.GetExtensionName(currentItem.id) + "=" + currentItem.level);
                        }
                        else
                        {
                            currentProperties.Add(""); // Fill in with empty
                        }
                    }

                    // Aggregate field, bonus 
                    var bonuses = input.RobotComponents.SelectMany(component => component.ExtensionBonuses).ToList(); // head.ExtensionBonuses.Concat(chassis.ExtensionBonuses).Concat(legs.ExtensionBonuses).ToList();

                    for (int i = 1; i <= 10; i++)
                    {
                        if (bonuses.Count >= i)
                        {
                            var currentItem = bonuses[i - 1];
                            currentProperties.Add(extensionReader.GetExtensionName(currentItem.extensionId) + "=" + currentItem.aggregateField + "+" + currentItem.bonus);
                        }
                        else
                        {
                            currentProperties.Add(""); // Fill in with empty
                        }
                    }

                    // We don't need this since we have the object already
                    // var extData2 = extensionReader.GetEnablerExtensions(input.Definition);


                    returnData.Add(currentProperties);

                }
                catch (Exception)
                {
                    returnData.Add(new List<string> { botName, "BIG EXCEPTION" });
                }
            }

            // now write all the data
            string filePath = outputPath;

            System.IO.File.WriteAllLines(filePath, returnData.Select(x => String.Join(",", x)));

        }

    }
}
