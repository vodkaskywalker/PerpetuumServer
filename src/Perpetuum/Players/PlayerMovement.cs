using System;

namespace Perpetuum.Players
{
    public class PlayerMovement
    {
        private static readonly TimeSpan minStepTime = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan maxElapsedTime = TimeSpan.FromSeconds(1);
        private readonly Player player;

        public PlayerMovement(Player player)
        {
            this.player = player;
        }

        public void Update(TimeSpan elapsed)
        {
            var speed = player.Speed;

            if (speed <= 0.0)
            {
                return;
            }

            elapsed = elapsed.Min(maxElapsedTime);

            var angle = player.Direction * MathHelper.PI2;
            var vx = Math.Sin(angle) * speed;
            var vy = Math.Cos(angle) * speed;
            var px = player.CurrentPosition.X;
            var py = player.CurrentPosition.Y;

            while (elapsed > TimeSpan.Zero)
            {
                var time = minStepTime;

                if (elapsed < minStepTime)
                {
                    time = elapsed;
                }

                elapsed -= minStepTime;

                var nx = px + (vx * time.TotalSeconds);
                var ny = py - (vy * time.TotalSeconds);

                var dx = (int)px - (int)nx;
                var dy = (int)py - (int)ny;

                if ((dx != 0 || dy != 0) &&
                    !player.IsWalkable((int)nx, (int)ny))
                {
                    break;
                }

                px = nx;
                py = ny;
            }

            player.TryMove(new Position(px, py));
        }
    }
}
