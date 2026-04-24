using System;
using SS15.Code.Modules.Atmospherics;

namespace SS15.Code.Core
{
    public class Map
    {
        public readonly int Width;
        public readonly int Height;
        public Tile[,] Tiles;

        public Map(int width, int height)
        {
            Width = width;
            Height = height;
            Tiles = new Tile[width, height];
            Generate();
        }

        void Generate()
        {
            Random rnd = new Random(42); // фиксированный сид для воспроизводимости
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    // Края карты — всегда стены
                    bool isWall = (x == 0 || y == 0 || x == Width - 1 || y == Height - 1);
                    if (!isWall)
                        isWall = rnd.NextDouble() < 0.3;  // 30% стен

                    Tiles[x, y] = new Tile(isWall);
                    if (!isWall)
                        Tiles[x, y].Air.SetToBreathable();  // начальный воздух
                }
            }

            // Очищаем стартовую зону вокруг центра
            ClearArea(Width / 2, Height / 2, 6);
        }

        void ClearArea(int cx, int cy, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int nx = cx + dx;
                    int ny = cy + dy;
                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                    {
                        Tiles[nx, ny].IsWall = false;
                        Tiles[nx, ny].Air.SetToBreathable();
                    }
                }
            }
        }
    }
}