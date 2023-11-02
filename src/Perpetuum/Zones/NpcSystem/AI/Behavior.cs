namespace Perpetuum.Zones.NpcSystem.AI
{
    public class Behavior
    {
        public BehaviorType Type { get; private set; }

        protected Behavior(BehaviorType type)
        {
            Type = type;
        }

        public static Behavior Create(BehaviorType type)
        {
            switch (type)
            {
                case BehaviorType.Neutral:
                    return new NeutralBehavior();
                case BehaviorType.Aggressive:
                    return new AggressiveBehavior();
                case BehaviorType.Passive:
                    return new PassiveBehavior();
                case BehaviorType.RemoteControlled:
                    return new RemoteControlledBehavior();
                default:
                    return new PassiveBehavior();
            }
        }
    }
}
