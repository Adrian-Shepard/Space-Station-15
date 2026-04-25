using System.Numerics;

namespace SS15.Common.World;

/// <summary>Игровая сущность (игрок, предмет, машина).</summary>
public sealed class Entity
{
    public uint Id { get; }
    public string Name { get; set; } = "";
    public Vector2 Position { get; set; }
    public string SpriteKey { get; set; } = ""; // ключ для спрайта в атласе

    public Entity(uint id)
    {
        Id = id;
    }
}