using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Services.Looting;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Units;
using Perpetuum.Zones.Eggs;
using Perpetuum.Zones.RemoteControl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Perpetuum.Zones.NpcSystem
{
    public class Npc : SmartCreature, ITaggable
    {
        private readonly TagHelper tagHelper;

        public Npc(TagHelper tagHelper)
        {
            this.tagHelper = tagHelper;
        }

        public NpcSpecialType SpecialType { get; set; }

        public int EP { get; private set; }

        public ILootGenerator LootGenerator { get; set; }

        public void Tag(Player tagger, TimeSpan duration)
        {
            tagHelper.DoTagging(this, tagger, duration);
        }

        [CanBeNull]
        public Player GetTagger()
        {
            return TagHelper.GetTagger(this);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public override IDictionary<string, object> GetDebugInfo()
        {
            var info = base.GetDebugInfo();
            var homeDistance = HomePosition.TotalDistance2D(CurrentPosition);

            info.Add("homePositionX", HomePosition.intX);
            info.Add("homePositionY", HomePosition.intY);
            info.Add("homeRange", HomeRange);
            info.Add("homeDistance", homeDistance);
            info.Add("coreMax", CoreMax);
            info.Add("coreCurrent", Core);
            info.Add("bestCombatRange", BestActionRange);

            var currentAI = AI.Current;

            if (currentAI != null)
            {
                info.Add("fsm", currentAI.GetType().Name);
            }

            info.Add("threat", ThreatManager.ToDebugString());
            Group?.AddDebugInfoToDictionary(info);
            info.Add("ismission", GetMissionGuid() != Guid.Empty);

            return info;
        }

        protected override void OnDead(Unit killer)
        {
            var zone = Zone;
            var tagger = GetTagger();

            Debug.Assert(zone != null, "zone != null");

            BossInfo?.OnDeath(this, killer);
            HandleNpcDeadAsync(zone, killer, tagger)
                .ContinueWith((t) => base.OnDead(killer))
                .LogExceptions();
        }

        public override bool IsWalkable(Vector2 position)
        {
            return Zone.IsWalkableForNpc((int)position.X, (int)position.Y, Slope);
        }

        public override bool IsWalkable(Position position)
        {
            return Zone.IsWalkableForNpc((int)position.X, (int)position.Y, Slope);
        }

        public override bool IsWalkable(int x, int y)
        {
            return Zone.IsWalkableForNpc(x, y, Slope);
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            SetEP(zone);

            base.OnEnterZone(zone, enterType);
        }

        private void SetEP(IZone zone)
        {
            if (zone.Configuration.Type == ZoneType.Training)
            {
                EP = 0;
                return;
            }

            var ep = NpcEp.GetEpForNpc(this);

            if (zone.Configuration.IsBeta)
                ep *= 2;

            EP = ep;
            Logger.DebugInfo($"Ep4Npc:{ep} def:{Definition} {ED.Name}");
        }

        public override string InfoString
        {
            get
            {
                var infoString = $"Npc:{ED.Name}:{Eid}";

                var zone = Zone;
                if (zone != null)
                {
                    infoString += " z:" + zone.Id;
                }

                if (Group != null)
                {
                    infoString += " g:" + Group.Name;
                }

                return infoString;
            }
        }

        public override bool IsHostile(Player player)
        {
            return true;
        }

        internal override bool IsHostile(AreaBomb bomb)
        {
            return true;
        }

        internal override bool IsHostile(SentryTurret turret)
        {
            return true;
        }

        internal override bool IsHostile(IndustrialTurret turret)
        {
            return true;
        }

        internal override bool IsHostile(CombatDrone drone)
        {
            return true;
        }

        protected override bool IsHostileFor(Unit unit)
        {
            return unit.IsHostile(this);
        }

        protected override void UpdateUnitVisibility(Unit target)
        {
            if (target is RemoteControlledCreature)
            {
                UpdateVisibility(target);
            }
        }

        private Task HandleNpcDeadAsync(IZone zone, Unit killer, Player tagger)
        {
            return Task.Run(() => HandleNpcDead(zone, killer, tagger));
        }

        private void HandleNpcDead([NotNull] IZone zone, Unit killer, Player tagger)
        {
            Logger.DebugInfo($"   >>>> NPC died.  Killer unitName:{killer.Name} o:{killer.Owner}   Tagger botname:{tagger?.Name} o:{killer.Owner} characterId:{tagger?.Character.Id}");

            using (var scope = Db.CreateTransaction())
            {
                if (BossInfo?.IsLootSplit ?? false)
                {
                    List<Player> participants = new List<Player>();
                    participants = ThreatManager.Hostiles.Select(x => zone.ToPlayerOrGetOwnerPlayer(x.Unit)).ToList();

                    if (participants.Count > 0)
                    {
                        ISplittableLootGenerator splitLooter = new SplittableLootGenerator(LootGenerator);
                        List<ILootGenerator> lootGenerators = splitLooter.GetGenerators(participants.Count);

                        for (var i = 0; i < participants.Count; i++)
                        {
                            LootContainer.Create()
                                .SetOwner(participants[i])
                                .AddLoot(lootGenerators[i])
                                .BuildAndAddToZone(zone, participants[i].CurrentPosition);
                        }
                    }
                }
                else
                {
                    LootContainer.Create().SetOwner(tagger).AddLoot(LootGenerator).BuildAndAddToZone(zone, CurrentPosition);
                }

                var killerPlayer = zone.ToPlayerOrGetOwnerPlayer(killer);

                if (GetMissionGuid() != Guid.Empty)
                {
                    Logger.DebugInfo("   >>>> NPC is mission related.");

                    SearchForMissionOwnerAndSubmitKill(zone, killer);
                }
                else
                {
                    Logger.DebugInfo("   >>>> independent NPC.");

                    if (killerPlayer != null)
                    {
                        EnqueueKill(killerPlayer, killer);
                    }
                }

                if (EP > 0)
                {
                    var awardedPlayers = new List<Unit>();

                    foreach (var hostile in ThreatManager.Hostiles.Where(x => x.Unit is Player))
                    {
                        var playerUnit = hostile.Unit;
                        var hostilePlayer = zone.ToPlayerOrGetOwnerPlayer(playerUnit);

                        hostilePlayer?.Character.AddExtensionPointsBoostAndLog(EpForActivityType.Npc, EP);
                        awardedPlayers.Add(playerUnit);
                    }

                    PseudoThreatManager.AwardPseudoThreats(awardedPlayers, zone, EP);
                }

                scope.Complete();
            }
        }

        /// <summary>
        /// This occurs when aoe kills the npc. 
        /// Background task that searches for the related missionguid and sumbits the kill for that specific player
        /// </summary>
        private void SearchForMissionOwnerAndSubmitKill(IZone zone, Unit killerUnit)
        {
            var missionGuid = GetMissionGuid();
            var missionOwner = MissionHelper.FindMissionOwnerByGuid(missionGuid);
            var missionOwnerPlayer = zone.GetPlayer(missionOwner);

            if (missionOwnerPlayer == null)
            {
                var info = new Dictionary<string, object>
                {
                    {k.characterID, missionOwner.Id},
                    {k.guid, missionGuid.ToString()},
                    {k.type, MissionTargetType.kill_definition},
                    {k.definition, ED.Definition},
                    {k.increase ,1},
                    {k.zoneID, zone.Id},
                    {k.position, killerUnit.CurrentPosition},
                };

                if (killerUnit is Player killerPlayer && killerPlayer.Character.Id != missionOwner.Id)
                {
                    info[k.assistingCharacterID] = killerPlayer.Character.Id;
                }

                Task.Run(() =>
                {
                    MissionHelper.MissionProcessor.NpcGotKilledInAway(missionOwner, missionGuid, info);
                });

                return;
            }

            EnqueueKill(missionOwnerPlayer, killerUnit);
        }

        private void EnqueueKill(Player missionOwnerPlayer, Unit killerUnit)
        {
            var eventSourcePlayer = missionOwnerPlayer;

            if (killerUnit is Player killerPlayer && !killerPlayer.Equals(missionOwnerPlayer))
            {
                eventSourcePlayer = killerPlayer;
            }

            Logger.DebugInfo($"   >>>> EventSource: botName:{eventSourcePlayer.Name} o:{eventSourcePlayer.Owner} characterId:{eventSourcePlayer.Character.Id} MissionOwner: botName:{missionOwnerPlayer.Name} o:{missionOwnerPlayer.Owner} characterId:{missionOwnerPlayer.Character.Id}");

            missionOwnerPlayer.MissionHandler.EnqueueMissionEventInfoLocally(new KillEventInfo(eventSourcePlayer, this, CurrentPosition));
        }
    }
}