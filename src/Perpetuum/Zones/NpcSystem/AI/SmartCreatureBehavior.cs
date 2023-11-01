namespace Perpetuum.Zones.NpcSystem.AI
{
    public class SmartCreatureBehavior
    {
        public SmartCreatureBehaviorType Type { get; private set; }

        protected SmartCreatureBehavior(SmartCreatureBehaviorType type)
        {
            Type = type;
        }

        public static SmartCreatureBehavior Create(SmartCreatureBehaviorType type)
        {
            switch (type)
            {
                case SmartCreatureBehaviorType.Neutral:
                    return new SmartCreatureNeutralBehavior();
                case SmartCreatureBehaviorType.Aggressive:
                    return new SmartCreatureAggressiveBehavior();
                case SmartCreatureBehaviorType.Passive:
                    return new SmartCreaturePassiveBehavior();
                case SmartCreatureBehaviorType.RemoteControlled:
                    return new SmartCreatureRemoteControlledBehavior();
                default:
                    return new SmartCreaturePassiveBehavior();
            }
        }
    }
}
