using SS15.Code.Core;
using System;

namespace SS15.Code.Systems
{
    public static class VisionSystem
    {
        public static void UpdateVisibility(Map map, int playerX, int playerY, int viewRadius)
        {
            // Сброс видимости всех клеток
            for (int x = 0; x < map.Width; x++)
                for (int y = 0; y < map.Height; y++)
                    map.Tiles[x, y].IsVisible = false;

            // Трассировка лучей из игрока
            double angleStep = Math.PI * 2 / 720;
            for (double angle = 0; angle < Math.PI * 2; angle += angleStep)
            {
                double dx = Math.Cos(angle);
                double dy = Math.Sin(angle);
                CastRay(map, playerX + 0.5, playerY + 0.5, dx, dy, viewRadius);
            }
            // Поле "Explored" больше не используется – никакой памяти
        }

        private static void CastRay(Map map, double ox, double oy, double dirX, double dirY, int viewRadius)
        {
            double x = ox, y = oy;
            for (int i = 0; i < viewRadius * 2; i++)
            {
                int tileX = (int)Math.Floor(x);
                int tileY = (int)Math.Floor(y);
                if (tileX < 0 || tileX >= map.Width || tileY < 0 || tileY >= map.Height) break;

                map.Tiles[tileX, tileY].IsVisible = true;
                if (map.Tiles[tileX, tileY].IsWall) break;

                x += dirX * 0.5;
                y += dirY * 0.5;
            }
        }
    }
}