using System.Collections.Generic;
using SS15.Common.World;

namespace SS15.Game.Objects;

public static class TilePrototypes
{
    public static readonly TilePrototype Floor = new(TileType.Floor, "Floors/floors");
    public static readonly TilePrototype Wall  = new(TileType.Wall,  "Walls/metal");

    public static Dictionary<TileType, string> ToDictionary()
    {
        return new Dictionary<TileType, string>
        {
            { Floor.Type, Floor.DmiKey },
            { Wall.Type,  Wall.DmiKey }
        };
    }
}
