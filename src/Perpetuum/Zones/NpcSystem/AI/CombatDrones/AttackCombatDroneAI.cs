using Perpetuum.Collections;
using Perpetuum.PathFinders;
using Perpetuum.Units;
using Perpetuum.Zones.Movements;
using Perpetuum.Zones.NpcSystem.TargettingStrategies;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Perpetuum.Zones.NpcSystem.AI.CombatDrones
{
    public class AttackCombatDroneAI : CombatAI
    {
        private Position lastTargetPosition;
        private PathMovement movement;
        private PathMovement nextMovement;
        private CancellationTokenSource source;
        private const int Sqrt2 = 141;
        private const int Weight = 1000;

        public AttackCombatDroneAI(SmartCreature smartCreature) : base(smartCreature) { }

        protected override CombatPrimaryLockSelectionStrategySelector InitSelector()
        {
            return CombatPrimaryLockSelectionStrategySelector.Create()
                .WithStrategy(CombatPrimaryLockSelectionStrategy.HostileOrClosest, 1)
                .Build();
        }

        public override void Exit()
        {
            this.source?.Cancel();

            base.Exit();
        }

        public override void Update(TimeSpan time)
        {
            //if (!smartCreature.IsInHomeRange)
            //{
            //    smartCreature.AI.Push(new HomingAI(smartCreature));

            //    return;
            //}

            if (!smartCreature.ThreatManager.IsThreatened)
            {
                ReturnToCommandBot();

                return;
            }

            this.UpdateHostile(time);

            base.Update(time);
        }

        protected override void ToAggressorAI() { }

        private void ReturnToCommandBot()
        {
            smartCreature.AI.Pop();
            smartCreature.AI.Push(new EscortCombatDroneAI(smartCreature));
            this.WriteLog("Returning to command robot.");
        }

        private void UpdateHostile(TimeSpan time)
        {
            var mostHated = GetPrimaryHostile();

            if (mostHated == null)
            {
                return;
            }

            if (!mostHated.Unit.CurrentPosition.IsEqual2D(this.lastTargetPosition))
            {
                this.lastTargetPosition = mostHated.Unit.CurrentPosition;

                var findNewTargetPosition = false;

                if (!smartCreature.IsInRangeOf3D(mostHated.Unit, smartCreature.BestActionRange))
                {
                    findNewTargetPosition = true;
                }
                else
                {
                    var visibility = smartCreature.GetVisibility(mostHated.Unit);

                    if (visibility != null)
                    {
                        var r = visibility.GetLineOfSight(this.IsNpcHasMissiles);

                        if (r.hit)
                        {
                            findNewTargetPosition = true;
                        }
                    }
                }

                if (findNewTargetPosition)
                {
                    FindNewAttackPositionAsync(mostHated.Unit).ContinueWith(t =>
                    {
                        if (t.IsCanceled)
                        {
                            return;
                        }

                        var path = t.Result;

                        if (path == null)
                        {
                            //smartCreature.ThreatManager.Remove(mostHated);
                            //smartCreature.AddPseudoThreat(mostHated.Unit);

                            return;
                        }

                        Interlocked.Exchange(ref this.nextMovement, new PathMovement(path));
                    });
                }
            }

            if (this.nextMovement != null)
            {
                movement = Interlocked.Exchange(ref this.nextMovement, null);
                movement.Start(this.smartCreature);
            }

            this.movement?.Update(this.smartCreature, time);
        }

        private Task<List<Point>> FindNewAttackPositionAsync(Unit hostile)
        {
            this.source?.Cancel();
            this.source = new CancellationTokenSource();

            return Task.Run(() => FindNewAttackPosition(hostile, this.source.Token), this.source.Token);
        }

        private List<Point> FindNewAttackPosition(Unit hostile, CancellationToken cancellationToken)
        {
            var end = hostile.CurrentPosition.GetRandomPositionInRange2D(0, smartCreature.BestActionRange - 1).ToPoint();

            smartCreature.StopMoving();

            var maxNode = Math.Pow(smartCreature.HomeRange, 2) * Math.PI;
            var priorityQueue = new PriorityQueue<Node>((int)maxNode);
            var startNode = new Node(smartCreature.CurrentPosition);

            priorityQueue.Enqueue(startNode);

            var closed = new HashSet<Point>
            {
                startNode.position
            };

            Node current;

            while (priorityQueue.TryDequeue(out current))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                if (IsValidAttackPosition(hostile, current.position))
                {
                    return BuildPath(current);
                }

                foreach (var n in current.position.GetNeighbours())
                {
                    if (closed.Contains(n))
                    {
                        continue;
                    }

                    closed.Add(n);

                    if (!smartCreature.IsWalkable(n.X, n.Y))
                    {
                        continue;
                    }

                    if (!n.IsInRange(smartCreature.HomePosition, smartCreature.HomeRange))
                    {
                        continue;
                    }

                    var newG = current.g + (n.X - current.position.X == 0 || n.Y - current.position.Y == 0 ? 100 : Sqrt2);
                    var newH = Heuristic.Manhattan.Calculate(n.X, n.Y, end.X, end.Y) * Weight;
                    var newNode = new Node(n)
                    {
                        g = newG,
                        f = newG + newH,
                        parent = current
                    };

                    priorityQueue.Enqueue(newNode);
                }
            }

            return null;
        }

        private bool IsValidAttackPosition(Unit hostile, Point position)
        {
            var position3 = smartCreature.Zone.FixZ(position.ToPosition()).AddToZ(smartCreature.Height);

            if (!hostile.CurrentPosition.IsInRangeOf3D(position3, smartCreature.BestActionRange))
            {
                return false;
            }

            var r = smartCreature.Zone.IsInLineOfSight(position3, hostile, false);

            return !r.hit;
        }

        private static List<Point> BuildPath(Node current)
        {
            var stack = new Stack<Point>();
            var node = current;

            while (node != null)
            {
                stack.Push(node.position);
                node = node.parent;
            }

            return stack.ToList();
        }
    }
}
