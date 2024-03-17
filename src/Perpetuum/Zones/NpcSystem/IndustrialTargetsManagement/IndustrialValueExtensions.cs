namespace Perpetuum.Zones.NpcSystem.IndustrialTargetsManagement
{
    public static class IndustrialValueExtensions
    {
        [CanBeNull]
        public static IndustrialTarget GetMostValuableIndustrialTarget(this IIndustrialValueManager manager)
        {
            return manager.IndustrialTargets.Min;
        }
    }
}
