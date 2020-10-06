using Autofac;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Org.BouncyCastle.Asn1.X509;
using Perpetuum.DataDumper.Views;
using Perpetuum.EntityFramework;
using Perpetuum.Log;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Perpetuum.DataDumper.DataDumper;

namespace Perpetuum.DataDumper {
    public partial class DumperForm : Form {
        public DumperForm() {
            InitializeComponent();
        }

        PerpetuumLightBootstrapper bootstrapper;

        private static List<DataExportMapping> mappings = new List<DataExportMapping>() {
                                // These universal ones are strange because they're stored in a different category
                                // even though in the game UI they're the same thing and they have basically the same stats
                                // Also the univeral one is a regular module while the others are Active
                                new DataExportMapping("ModuleArmorHardenerStats", typeof(ModuleArmorHardenerDataView), "cf_armor_hardeners"),
                                new DataExportMapping("ModuleShieldGeneratorStats", typeof(ModuleShieldGeneratorDataView), "cf_shield_generators"),
                                new DataExportMapping("ModuleERPStats", typeof(ModuleERPDataView), "cf_kers"),
                                new DataExportMapping("ModuleRemArmorRepairerStats", typeof(ModuleRemoteArmorRepairerDataView), "cf_remote_armor_repairers"),
                                new DataExportMapping("ModuleArmorPlateStats", typeof(ModuleArmorPlateDataView), "cf_armor_plates"),
                                new DataExportMapping("SparkStats", typeof(SparkDataView), ""),
                                new DataExportMapping("ModuleArmorRepairerStats", typeof(ModuleArmorRepairerDataView), "cf_armor_repair_systems"),
                                new DataExportMapping("AmmoWeaponStats", typeof(AmmoWeaponDataView), "cf_ammo"),
                                new DataExportMapping("WeaponStats", typeof(ModuleWeaponDataView), "cf_weapons")
                            };

        private string serverRoot = @"C:\PerpetuumServer\data";

        private void Form1_Load(object sender, EventArgs e) {
            serverPathTextbox.Text = serverRoot;

            foreach (var item in mappings) {
                mappingSelectList.Items.Add(item, true);
            }
        }


        private void StartButton_Click(object sender, EventArgs e) {
            var tableGroupings = mappingSelectList.CheckedItems.Cast<DataExportMapping>().GroupBy(x => x.TableName).ToList();

            if (!System.IO.Directory.Exists(serverRoot)) {
                MessageBox.Show("Server path doesn't exist: " + serverRoot);
            }

            bootstrapper = new PerpetuumLightBootstrapper();

            bootstrapper.Init(serverRoot);

            bootstrapper.InitDumper();

            IWorkbook workbook = new XSSFWorkbook();

            ISheet definitionsSheet = workbook.CreateSheet("Definitions");

            int currentDefinitionRow = 0;
            var headerRow = definitionsSheet.CreateRow(currentDefinitionRow);
            headerRow.CreateCell(0).SetCellValue("Type");
            headerRow.CreateCell(1).SetCellValue("Definition");
            currentDefinitionRow++;

            // We need to group by the Cargo Table because we may have multipler
            // definitions writing into the same table.
            
            foreach (var tableGroup in tableGroupings) {
                // Write the definition for the group

                // Use the first view for the definition since they should all be the same
                var typeToWrite = tableGroup.First();

                string currentDefinition = DataDumper.GenerateCargoDefinition(typeToWrite.ViewType, typeToWrite.TableName);

                var currentRow = definitionsSheet.CreateRow(currentDefinitionRow);
                currentRow.CreateCell(0).SetCellValue(typeToWrite.TableName);
                currentRow.CreateCell(1).SetCellValue(currentDefinition);

                currentDefinitionRow++;

                int currentDataRow = 0;
                ISheet currentDataSheet = workbook.CreateSheet(typeToWrite.TableName);

                foreach (DataExportMapping item in tableGroup) {
                    dynamic currentData = bootstrapper.Dumper.DumpDataView(item);

                    var currentRows = bootstrapper.Dumper.ComposeDataView(currentData, item.TableName);

                    bootstrapper.Dumper.WriteDataView(currentRows, item.TableName, currentDataSheet, ref currentDataRow);
                }
            }

            string filePath = $"dataDump_{DateTime.Now.ToString("yy-MM-dd_hh-mm-ss")}.xlsx";

            workbook.Write(new FileStream(filePath, FileMode.CreateNew));

            MessageBox.Show($"File exported to:\n{filePath}");

        }

        private void allTypesButton_Click(object sender, EventArgs e) {
            for (int i = 0; i < mappings.Count; i++) {
                mappingSelectList.SetItemChecked(i, true);
            }
        }

        private void clearTypesButton_Click(object sender, EventArgs e) {
            for (int i = 0; i < mappings.Count; i++) {
                mappingSelectList.SetItemChecked(i, false);
            }
        }

        private void serverPathTextbox_TextChanged(object sender, EventArgs e) {
            serverRoot = serverPathTextbox.Text;
        }
    }
}
