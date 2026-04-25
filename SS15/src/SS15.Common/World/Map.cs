using SS15.Common.Math;

namespace SS15.Common.World;

public sealed class Map
{
    public int Width { get; }
    public int Height { get; }
    private readonly TileDef[] _tiles;

    public Map(int width, int height)
    {
        Width = width;
        Height = height;
        _tiles = new TileDef[width * height];
    }

    public TileDef GetTile(int x, int y) => _tiles[x + y * Width];
    public void SetTile(int x, int y, TileDef tile) => _tiles[x + y * Width] = tile;
    public TileDef GetTile(Vector2i pos) => GetTile(pos.X, pos.Y);
    public void SetTile(Vector2i pos, TileDef tile) => SetTile(pos.X, pos.Y, tile);

    public bool IsInBounds(int x, int y) =>
        x >= 0 && x < Width && y >= 0 && y < Height;

    public bool IsTileSolid(int x, int y)
    {
        if (!IsInBounds(x, y)) return true;
        return GetTile(x, y).Type != TileType.Floor;
    }
}