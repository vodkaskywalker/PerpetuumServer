namespace Perpetuum.Zones.NpcSystem.IndustrialTargetsManagement
{
    public struct IndustrialValue
    {
        public const double CommonMineral = 10;
        public const double RareMineral = 25;
        public const double CommonPlant = 10;
        public const double RarePlant = 25;
        public readonly IndustrialValueType type;
        public readonly double value;

        public IndustrialValue(IndustrialValueType type, double value)
        {
            this.type = type;
            this.value = value;
        }

        public override string ToString()
        {
            return $"{type} = {value}";
        }

        public static IndustrialValue Multiply(IndustrialValue threat, double multiplier)
        {
            return new IndustrialValue(threat.type, threat.value * multiplier);
        }
    }
}
