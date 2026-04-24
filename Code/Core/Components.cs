namespace SS15.Code.Core
{
    public struct Position   // бывший Transform
    {
        public int X, Y;
        public Position(int x, int y) { X = x; Y = y; }
    }

    public struct Renderable
    {
        public string TexturePath;
        public int SortOrder;
        public Renderable(string path, int order = 0) { TexturePath = path; SortOrder = order; }
    }

    public struct Item
    {
        public string Name;
        public string Description;
        public bool CanPickup;
        public bool CanUseOnTile;
        public Item(string name, string desc = "", bool canPickup = true, bool canUse = false)
        {
            Name = name; Description = desc; CanPickup = canPickup; CanUseOnTile = canUse;
        }
    }
}