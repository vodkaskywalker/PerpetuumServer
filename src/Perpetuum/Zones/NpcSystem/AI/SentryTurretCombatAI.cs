namespace Perpetuum.Zones.NpcSystem.AI
{
    public class SentryTurretCombatAI : StationaryCombatAI
    {
        public SentryTurretCombatAI(SmartCreature smartCreature) : base(smartCreature) { }

        protected override PrimaryLockSelectionStrategySelector InitSelector()
        {
            return PrimaryLockSelectionStrategySelector.Create()
                .WithStrategy(PrimaryLockSelectionStrategy.HostileOrClosest, 1)
                .Build();

            //return PrimaryLockSelectionStrategySelector.Create()
            //    .WithStrategy(PrimaryLockSelectionStrategy.Hostile, 1)
            //    .WithStrategy(PrimaryLockSelectionStrategy.Closest, 2)
            //    .WithStrategy(PrimaryLockSelectionStrategy.OptimalRange, 3)
            //    .WithStrategy(PrimaryLockSelectionStrategy.Random, 10)
            //    .Build();
        }
    }
}
