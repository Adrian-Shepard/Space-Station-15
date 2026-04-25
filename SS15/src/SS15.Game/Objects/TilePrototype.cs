using SS15.Common.World;

namespace SS15.Game.Objects;

public sealed class TilePrototype
{
    public TileType Type { get; }
    public string DmiKey { get; }   // ключ для ResourceManager

    public TilePrototype(TileType type, string dmiKey)
    {
        Type = type;
        DmiKey = dmiKey;
    }
}
