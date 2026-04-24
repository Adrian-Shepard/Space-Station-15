using System.Collections.Generic;
using SS15.Code.Modules.Atmospherics;
using SS15.Code.Systems;

namespace SS15.Code.Core
{
    public class GameState
    {
        public Map Map { get; set; }
        public LINDA Linda { get; set; }
        public int PlayerX { get; set; }
        public int PlayerY { get; set; }
        public int TileSize { get; set; } = 32;
        public int ViewRadius { get; set; } = 12;
        public bool FogOfWar { get; set; } = true;
        public int TargetFPS { get; set; } = 60;
        public bool IsFullscreen { get; set; } = false;
        public bool MenuOpen { get; set; } = false;

        // Слоты экипировки
        public Entity? LeftHand { get; set; }
        public Entity? RightHand { get; set; }
        public Entity? Pocket1 { get; set; }
        public Entity? Pocket2 { get; set; }
        public bool ActiveLeftHand { get; set; } = true;
        public Entity? ActiveHandItem => ActiveLeftHand ? LeftHand : RightHand;

        public List<Entity> WorldEntities { get; set; } = new();
        public Dictionary<string, Raylib_cs.Texture2D> ItemTextures { get; set; } = new();

        // Чат
        public List<(string Sender, string Text)> ChatMessages { get; set; } = new();
        public bool ChatInputActive { get; set; } = false;  // фокус на вводе чата
        public string CurrentChatText { get; set; } = "";

        // Настройки
        public int SettingsTab { get; set; } = 0;   // 0 = Game, 1 = Settings

        public GameState(int mapSize = 256)
        {
            Map = new Map(mapSize, mapSize);
            Linda = new LINDA(Map);
            PlayerX = Map.Width / 2;
            PlayerY = Map.Height / 2;
            VisionSystem.UpdateVisibility(Map, PlayerX, PlayerY, ViewRadius);
        }
    }
}