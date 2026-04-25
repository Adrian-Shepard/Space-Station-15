using SS15.Common.World;

namespace SS15.Game.Mobs;

/// <summary>Базовый прототип моба (персонажа, животного и т.п.)</summary>
public abstract class MobPrototype
{
    public abstract string Name { get; }
    public abstract string DmiPath { get; }        // путь к DMI-файлу
    public abstract string StateName { get; }      // основное состояние (world)
    public abstract int BaseDirection { get; }     // начальное направление (1-4)

    /// <summary>Создаёт сущность игрока на основе прототипа.</summary>
    public Entity CreateEntity(uint id)
    {
        string state = string.IsNullOrEmpty(StateName) ? "" : $":{StateName}";
        return new Entity(id)
        {
            Name = Name,
            SpriteKey = $"{DmiPath}{state}:{BaseDirection}:0"
        };
    }
}
