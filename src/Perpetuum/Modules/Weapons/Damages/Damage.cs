namespace Perpetuum.Modules.Weapons.Damages
{
    public struct Damage
    {
        public readonly DamageType type;
        public readonly double value;

        public Damage(DamageType type, double value)
        {
            this.type = type;
            this.value = value;
        }
    }
}