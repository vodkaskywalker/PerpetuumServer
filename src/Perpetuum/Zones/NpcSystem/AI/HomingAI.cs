using Perpetuum.PathFinders;
using Perpetuum.Zones.Movements;
using System;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class HomingAI : CombatAI
    {
        private PathMovement movement;
        private readonly double maxReturnHomeRadius;
        private readonly PathFinder pathFinder;

        public HomingAI(SmartCreature smartCreature) : base(smartCreature)
        {
            this.maxReturnHomeRadius = (smartCreature.HomeRange * 0.4).Clamp(3, 20);
            this.pathFinder = new AStarFinder(Heuristic.Manhattan, smartCreature.IsWalkable);
        }

        public override void Enter()
        {
            var randomHome = this.smartCreature.Zone.FindPassablePointInRadius(this.smartCreature.HomePosition, (int)this.maxReturnHomeRadius);

            if (randomHome == default)
            {
                randomHome = this.smartCreature.HomePosition;
            }

            pathFinder
                .FindPathAsync(this.smartCreature.CurrentPosition, randomHome)
                .ContinueWith(t =>
                {
                    var path = t.Result;

                    if (path == null)
                    {
                        WriteLog("Path not found! (" + this.smartCreature.CurrentPosition + " => " + this.smartCreature.HomePosition + ")");

                        var f = new AStarFinder(Heuristic.Manhattan, (x, y) => true);

                        path = f.FindPath(this.smartCreature.CurrentPosition, this.smartCreature.HomePosition);

                        if (path == null)
                        {
                            WriteLog("Safe path not found! (" + this.smartCreature.CurrentPosition + " => " + this.smartCreature.HomePosition + ")");
                        }
                    }

                    this.movement = new PathMovement(path);
                    this.movement.Start(this.smartCreature);
                });

            base.Enter();
        }

        public override void Update(TimeSpan time)
        {
            if (this.movement != null)
            {
                this.movement.Update(this.smartCreature, time);

                if (this.movement.Arrived)
                {
                    this.smartCreature.AI.Pop();

                    return;
                }
            }

            base.Update(time);
        }

        protected override void ToHomeAI() { }
        protected override void ToAggressorAI() { }
    }
}
