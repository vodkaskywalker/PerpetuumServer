using Perpetuum.Data;
using Perpetuum.Services.EventServices;
using Perpetuum.Services.RiftSystem;
using Perpetuum.Zones.NpcSystem.Flocks;
using System.Data;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem
{
    public class NpcBossInfoBuilder
    {
        private readonly ICustomRiftConfigReader _customRiftConfigReader;
        private readonly EventListenerService _eventChannel;

        public NpcBossInfoBuilder(ICustomRiftConfigReader customRiftConfigReader, EventListenerService eventChannel)
        {
            _customRiftConfigReader = customRiftConfigReader;
            _eventChannel = eventChannel;
        }

        public NpcBossInfo CreateBossInfoFromDB(IDataRecord record)
        {
            var id = record.GetValue<int>("id");
            var flockid = record.GetValue<int>("flockid");
            var respawnFactor = record.GetValue<double?>("respawnNoiseFactor");
            var lootSplit = record.GetValue<bool>("lootSplitFlag");
            var outpostEID = record.GetValue<long?>("outpostEID");
            var stabilityPts = record.GetValue<int?>("stabilityPts");
            var overrideRelations = record.GetValue<bool>("overrideRelations");
            var deathMessage = record.GetValue<string>("customDeathMessage");
            var aggressMessage = record.GetValue<string>("customAggressMessage");
            var riftConfigId = record.GetValue<int?>("riftConfigId");
            var riftConfig = _customRiftConfigReader.GetById(riftConfigId ?? -1);
            var announce = record.GetValue<bool>("isAnnounced");
            var info = new NpcBossInfo(
                _eventChannel,
                id,
                flockid,
                respawnFactor,
                lootSplit,
                outpostEID,
                stabilityPts,
                overrideRelations,
                deathMessage,
                aggressMessage,
                riftConfig,
                announce
             );

            return info;
        }

        public NpcBossInfo GetBossInfoByFlockID(int flockid, IFlockConfiguration config)
        {
            var bossInfos = Db.Query()
                .CommandText(@"SELECT TOP 1 id, flockid, respawnNoiseFactor, lootSplitFlag, outpostEID,
                    stabilityPts, overrideRelations, customDeathMessage, customAggressMessage, riftConfigId,
                    isAnnounced
                    FROM dbo.npcbossinfo WHERE flockid=@flockid;")
                .SetParameter("@flockid", flockid)
                .Execute()
                .Select(CreateBossInfoFromDB);

            var info = bossInfos.SingleOrDefault();

            if (info == null)
            {
                return null;
            }

            info.RespawnTime = config.RespawnTime;

            return info;
        }
    }
}
