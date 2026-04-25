using SS15.Common.World;

namespace SS15.Game.Mobs;

public sealed class CatMob : MobPrototype
{
    public override string Name => "cat_calico";
    public override string DmiPath => "Assets/Sprites/Mobs/cat_calico";
    public override string StateName => "world";
    public override int BaseDirection => 1;
}
