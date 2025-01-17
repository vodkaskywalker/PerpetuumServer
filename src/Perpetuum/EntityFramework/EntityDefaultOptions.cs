using Perpetuum.ExportedTypes;
using Perpetuum.GenXY;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.RemoteControl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Perpetuum.EntityFramework
{
    public sealed class EntityDefaultOptions
    {
        private readonly IDictionary<string, object> _dictionary = new Dictionary<string, object>();

        public EntityDefaultOptions() { }

        public EntityDefaultOptions(IDictionary<string, object> dictionary)
        {
            _dictionary = dictionary;
        }

        public TReturnType GetOption<TReturnType>(string keyName)
        {
            return _dictionary.GetOrDefault<TReturnType>(keyName);
        }

        public int AlarmPeriod => _dictionary.GetOrDefault<int>(k.alarmPeriod);

        public bool PublicBeam => _dictionary.GetOrDefault<int>(k.publicBeam) == 1;

        public int Points => _dictionary.GetOrDefault<int>(k.points);

        public int ProductionTime => _dictionary.GetOrDefault<int>(k.productionTime);

        public int Level => _dictionary.GetOrDefault<int>(k.level);

        public double PerSecondPrice => _dictionary.GetOrDefault<double>(k.perSecondPrice);

        public double BulletTime => _dictionary.GetOrDefault<double>(k.bulletTime);

        public string MineralLayer => _dictionary.GetOrDefault<string>(k.mineral);

        public double Capacity => _dictionary.GetOrDefault<double>(k.capacity);

        public int AmmoCapacity => _dictionary.GetOrDefault<int>(k.ammoCapacity);

        public int ModuleFlag => _dictionary.GetOrDefault<int>(k.moduleFlag);

        [NotNull]
        public int[] SlotFlags => _dictionary.GetOrDefault<int[]>(k.slotFlags) ?? new int[0];

        public Position SpawnPosition
        {
            get
            {
                int x = _dictionary.GetOrDefault<int>(k.spawnPositionX);
                int y = _dictionary.GetOrDefault<int>(k.spawnPositionY);
                return new Position(x, y);
            }
        }

        public int SpawnRange => _dictionary.GetOrDefault<int>(k.spawnRange);

        public int Size => _dictionary.GetOrDefault<int>(k.size);

        public int DockingRange => _dictionary.GetOrDefault<int>(k.dockRange);

        public int Max => _dictionary.GetOrDefault<int>(k.max);

        public int Increase => _dictionary.GetOrDefault<int>(k.increase);

        public double Height => _dictionary.GetOrDefault(k.height, 1.0);

        public int Item => _dictionary.GetOrDefault<int>(k.item);

        [NotNull]
        public EffectType[] Effects => _dictionary.GetOrDefault<int[]>(k.effect).Select(e => (EffectType)e).ToArray();

        public int Type => _dictionary.GetOrDefault<int>(k.type);

        public string Tier => _dictionary.GetOrDefault<string>("tier");

        public TechTreePointType KernelPointType => _dictionary.GetOrDefault<TechTreePointType>("pointType");

        public string ToGenxyString()
        {
            return GenxyConverter.Serialize(_dictionary);
        }

        public int ExtensionPoints
        {
            get
            {
                int ep = _dictionary.GetOrDefault("ep", 0);
                Debug.Assert(ep > 0);
                return ep;
            }
        }

        public int Credit
        {
            get
            {
                int credit = _dictionary.GetOrDefault("credit", 0);
                Debug.Assert(credit > 0);
                return credit;
            }
        }

        public int SparkID
        {
            get
            {
                int id = _dictionary.GetOrDefault("sparkId", 0);
                Debug.Assert(id > 0);
                return id;
            }
        }

        public int TurretId
        {
            get
            {
                int id = _dictionary.GetOrDefault(k.TurretId, 0);
                Debug.Assert(id > 0);

                return id;
            }
        }

        public int PackedTurretId
        {
            get
            {
                int id = _dictionary.GetOrDefault(k.PackedTurretId, 0);
                Debug.Assert(id > 0);

                return id;
            }
        }

        public TurretType TurretType
        {
            get
            {
                string typeString = _dictionary.GetOrDefault<string>("turretType");

                return (TurretType)Enum.Parse(typeof(TurretType), typeString);
            }
        }

        public Faction Faction
        {
            get
            {
                string typeString = _dictionary.GetOrDefault<string>("faction");

                return typeString != null ?
                    (Faction)Enum.Parse(typeof(Faction), typeString)
                    : Faction.Niani;
            }
        }

        public int PlasmaDefinition => _dictionary.GetOrDefault<int>(k.PlasmaDefinition);

        public int PlasmaConsumption => _dictionary.GetOrDefault<int>(k.PlasmaConsumption);
    }
}