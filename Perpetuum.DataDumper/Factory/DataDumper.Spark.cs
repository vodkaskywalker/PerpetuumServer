using Perpetuum.DataDumper.Views;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Alliances;
using Perpetuum.Items.Ammos;
using Perpetuum.Services.ExtensionService;
using Perpetuum.Services.Sparks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Perpetuum.DataDumper.DataDumper;

namespace Perpetuum.DataDumper { 
    public partial class DataDumper {
        private string SparkBonusString(string extensionName, int value) {
            var formulaStrings = new List<string> { "{%BONUS100%}", "{%BONUS%}" };

            // First check if the localized name exists
            var localName = GetLocalizedName(extensionName);

            if (localName == extensionName) {
                // This means the desc wasn't found
                localName = GetLocalizedName(extensionName + "_desc");

                foreach (var formula in formulaStrings) {
                    localName = localName.Replace(formula, value.ToString());
                }

            }

            return localName;
        }

        public SparkDataView NewSparkDataView(Spark spark) {
            var view = new SparkDataView();

            view.item_name = GetLocalizedName(spark.sparkName);
            view.item_key = spark.sparkName;
            // view.item_categories
            view.price_equip = spark.changePrice;
            view.price_purchase = spark.unlockPrice;
            view.required_standing = spark.standingLimit * 100;
            view.sequence = spark.displayOrder;
            view.icon = spark.icon;
            view.energy_prop = spark.energyCredit.ToString();

            if (spark.allianceEid.HasValue) {
                // For some reason the Alliance object doesn't pull over the name
                // so we need to pull it as an eneity
                // var currentAlliance = Alliance.Repository.Load(spark.allianceEid);

                // Trying as an entity doesn't work either
                // var currentAlliance = entityServices.Repository.Load(.Value);


                var allianceData = AllianceHelper.GetAllianceInfo();

                foreach (var allianceInfo in allianceData) {
                    var childDict = (Dictionary<string, object>) allianceInfo.Value;
                    long allianceEid = (long) childDict["allianceEID"];
                    string allianceName = (string) childDict["name"];

                    if (allianceEid == spark.allianceEid) {
                        view.alliance = GetLocalizedName(allianceName);
                        break;
                    }
                }
            }

            var extensions = new Dictionary<string, int>();
            var extensionStrings = new List<string>();

            foreach (Extension extension in spark.RelatedExtensions) {
                var extensionInfo = extensionReader.GetExtensionByID(extension.id);

                extensions.Add(extensionInfo.name, extension.level);

                string localName = SparkBonusString(extensionInfo.name, extension.level);
                
                extensionStrings.Add(localName);

            }

            view.extensions = String.Join(";", extensionStrings);

            return view;
        }
    }
}