using Perpetuum.Builders;
using Perpetuum.Units;

namespace Perpetuum.Modules.Weapons.Damages
{
    public interface IDamageBuilder : IBuilder<DamageInfo>
    {
        IDamageBuilder WithAttacker(Unit attacker);

        IDamageBuilder WithAttackerWithoutPosition(Unit attacker);

        IDamageBuilder WithSourcePosition(Position position);

        IDamageBuilder WithExplosionRadius(double explosionRadius);

        IDamageBuilder WithOptimalRange(double optimalRange);

        IDamageBuilder WithFalloff(double falloff);

        IDamageBuilder WithDamage(Damage damage);
    }
}
