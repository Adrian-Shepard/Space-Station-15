namespace SS15.Common.World;

public enum TileType : byte { Space = 0, Floor = 1, Wall = 2 }
public readonly record struct TileDef(TileType Type, byte Variant);