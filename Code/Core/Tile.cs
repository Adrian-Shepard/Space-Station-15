using SS15.Code.Modules.Atmospherics;

namespace SS15.Code.Core
{
    public struct Tile
    {
        public bool IsWall;          // непроходимая стена
        public bool IsVisible;       // видим ли сейчас (в зоне обзора)
        public bool IsExplored;      // был ли когда-то видим (туман войны)
        public GasMixture Air;       // состояние газов в тайле

        public Tile(bool isWall)
        {
            IsWall = isWall;
            IsVisible = false;
            IsExplored = false;
            Air = new GasMixture();
        }
    }
}