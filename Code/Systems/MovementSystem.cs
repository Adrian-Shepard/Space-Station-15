using SS15.Code.Core;

namespace SS15.Code.Systems
{
    public static class MovementSystem
    {
        public static bool TryMove(Map map, ref int x, ref int y, int dx, int dy)
        {
            int nx = x + dx;
            int ny = y + dy;
            if (nx >= 0 && nx < map.Width && ny >= 0 && ny < map.Height && !map.Tiles[nx, ny].IsWall)
            {
                x = nx;
                y = ny;
                return true;
            }
            return false;
        }
    }
}