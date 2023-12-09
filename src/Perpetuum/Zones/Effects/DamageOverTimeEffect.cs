using Perpetuum.Modules.Weapons.Damages;

namespace Perpetuum.Zones.Effects
{
    public class DamageOverTimeEffect : Effect
    {
        protected override void OnTick()
        {
            var damageInfo = new DamageBuilder().
                WithDamage(new Damage(DamageType.Acid, this.DamagePerTick))
                .Build();

            Owner.TakeDamage(damageInfo);
        }
    }
}
