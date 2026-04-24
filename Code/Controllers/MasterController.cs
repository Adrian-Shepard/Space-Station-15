// =============================================================================
// Code/Controllers/MasterController.cs
// =============================================================================
using Raylib_cs;
using SS15.Code.Core;
using SS15.Code.Modules.Atmospherics;
using SS15.Code.Systems;
using System;
using System.Diagnostics;
using System.IO;

namespace SS15.Code.Controllers
{
    public class MasterController
    {
        private GameState state;
        private Stopwatch stopwatch = new();
        private double lastFrameTimeMs, lastAtmosTimeMs, lastVisionTimeMs, lastRenderTimeMs;
        private int fps, frameCount;
        private double fpsTimer;

        private Texture2D wallTex, floorTex, playerTex;
        private Font chatFont;

        public MasterController(int initialSize = 256)
        {
            state = new GameState(initialSize);
            SpawnTestItems();
        }

        private void SpawnTestItems()
        {
            var items = new (string name, string tex, bool canUse)[] {
                ("Wrench", "wrench.png", false),
                ("Crowbar", "crowbar.png", true),
                ("Fire Extinguisher", "extinguisher.png", true),
                ("Test Item", "test.png", false)
            };
            int startX = state.PlayerX - 2, startY = state.PlayerY - 2;
            for (int i = 0; i < items.Length; i++)
            {
                int x = startX + i, y = startY;
                if (x < 0 || x >= state.Map.Width || y < 0 || y >= state.Map.Height) continue;
                if (state.Map.Tiles[x, y].IsWall) continue;

                var entity = new Entity { Id = state.WorldEntities.Count + 1 };
                entity.SetComponent(new Position(x, y));
                entity.SetComponent(new Renderable(items[i].tex, 1));
                entity.SetComponent(new Item(items[i].name, "", true, items[i].canUse));
                state.WorldEntities.Add(entity);
            }
        }

        private Texture2D LoadFirstTextureFromFolder(string folderPath, Color fallback)
        {
            if (Directory.Exists(folderPath))
            {
                var files = Directory.GetFiles(folderPath, "*.png");
                if (files.Length > 0) return Raylib.LoadTexture(files[0]);
            }
            Image img = Raylib.GenImageColor(16, 16, fallback);
            Texture2D tex = Raylib.LoadTextureFromImage(img);
            Raylib.UnloadImage(img);
            return tex;
        }

        private Font LoadFont()
        {
            string fontPath = "Assets/Fonts/arial.ttf";
            if (File.Exists(fontPath)) return Raylib.LoadFont(fontPath);
            return Raylib.GetFontDefault();
        }

        public void Run()
        {
            Raylib.InitWindow(800, 600, "SS15 – Interactive Inventory");
            Raylib.SetTargetFPS(state.TargetFPS);
            Raylib.SetExitKey(0);

            wallTex   = LoadFirstTextureFromFolder("Assets/Textures/walls",  Color.DarkGray);
            floorTex  = LoadFirstTextureFromFolder("Assets/Textures/floors", Color.White);
            playerTex = LoadFirstTextureFromFolder("Assets/Textures/player", Color.Yellow);
            chatFont  = LoadFont();

            stopwatch.Start();
            int atmosTimer = 0;

            while (!Raylib.WindowShouldClose())
            {
                double frameStart = stopwatch.Elapsed.TotalMilliseconds;

                // ── Чат ──
                if (state.ChatInputActive)
                {
                    int ch = Raylib.GetCharPressed();
                    while (ch != 0)
                    {
                        if (ch >= 32 && ch <= 0x10FFFF)
                            state.CurrentChatText += (char)ch;
                        ch = Raylib.GetCharPressed();
                    }

                    if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && state.CurrentChatText.Length > 0)
                        state.CurrentChatText = state.CurrentChatText.Remove(state.CurrentChatText.Length - 1);

                    if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                    {
                        ChatSystem.SendPlayerMessage(state, state.CurrentChatText);
                        state.CurrentChatText = "";
                        state.ChatInputActive = false;
                    }

                    if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                    {
                        state.CurrentChatText = "";
                        state.ChatInputActive = false;
                    }
                }

                // ── Меню по Esc (только если не чат) ──
                if (!state.ChatInputActive && Raylib.IsKeyPressed(KeyboardKey.Escape))
                {
                    state.MenuOpen = !state.MenuOpen;
                    if (state.MenuOpen) state.SettingsTab = 0;
                }

                // Открыть чат по T
                if (!state.ChatInputActive && !state.MenuOpen && Raylib.IsKeyPressed(KeyboardKey.T))
                {
                    state.ChatInputActive = true;
                    state.CurrentChatText = "";
                }

                // ── Игровой мир (WASD, предметы, атмосфера) ──
                if (!state.MenuOpen && !state.ChatInputActive)
                {
                    int newX = state.PlayerX, newY = state.PlayerY;
                    bool moved = false;
                    if (Raylib.IsKeyPressed(KeyboardKey.D)) moved = MovementSystem.TryMove(state.Map, ref newX, ref newY, 1, 0);
                    if (Raylib.IsKeyPressed(KeyboardKey.A)) moved = MovementSystem.TryMove(state.Map, ref newX, ref newY, -1, 0);
                    if (Raylib.IsKeyPressed(KeyboardKey.S)) moved = MovementSystem.TryMove(state.Map, ref newX, ref newY, 0, 1);
                    if (Raylib.IsKeyPressed(KeyboardKey.W)) moved = MovementSystem.TryMove(state.Map, ref newX, ref newY, 0, -1);

                    if (moved)
                    {
                        state.PlayerX = newX; state.PlayerY = newY;
                        double visStart = stopwatch.Elapsed.TotalMilliseconds;
                        VisionSystem.UpdateVisibility(state.Map, state.PlayerX, state.PlayerY, state.ViewRadius);
                        lastVisionTimeMs = stopwatch.Elapsed.TotalMilliseconds - visStart;
                    }

                    int camX = state.PlayerX * state.TileSize - 400 + state.TileSize / 2;
                    int camY = state.PlayerY * state.TileSize - 300 + state.TileSize / 2;
                    int mx = (Raylib.GetMouseX() + camX) / state.TileSize;
                    int my = (Raylib.GetMouseY() + camY) / state.TileSize;

                    // Взаимодействие мышью
                    if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                    {
                        if (RenderSystem.IsMouseOverUI())
                            RenderSystem.HandleInventoryClick(state);
                        else
                            InteractionSystem.TryPickupAt(state, mx, my);
                    }

                    if (Raylib.IsKeyPressed(KeyboardKey.Z))
                        InteractionSystem.UseActiveItem(state, mx, my);
                    if (Raylib.IsKeyPressed(KeyboardKey.X))
                        InventorySystem.SwitchHand(state);
                    if (Raylib.IsKeyPressed(KeyboardKey.Q))
                        InventorySystem.DropActiveHand(state, state.PlayerX, state.PlayerY);
                    // Клавиша C больше не используется для карманов

                    // Атмосфера
                    if (mx >= 0 && mx < state.Map.Width && my >= 0 && my < state.Map.Height)
                    {
                        var tileVisible = (!state.FogOfWar || state.Map.Tiles[mx, my].IsVisible);
                        if (tileVisible && !state.Map.Tiles[mx, my].IsWall)
                        {
                            ref var air = ref state.Map.Tiles[mx, my].Air;
                            const float dGas = 0.5f, dTemp = 10f;
                            if (Raylib.IsKeyPressed(KeyboardKey.O)) air.MolesO2 += dGas;
                            if (Raylib.IsKeyPressed(KeyboardKey.P)) air.MolesO2 = Math.Max(0, air.MolesO2 - dGas);
                            if (Raylib.IsKeyPressed(KeyboardKey.K)) air.MolesN2 += dGas;
                            if (Raylib.IsKeyPressed(KeyboardKey.L)) air.MolesN2 = Math.Max(0, air.MolesN2 - dGas);
                            if (Raylib.IsKeyPressed(KeyboardKey.N)) air.Temperature += dTemp;
                            if (Raylib.IsKeyPressed(KeyboardKey.M)) air.Temperature = Math.Max(1f, air.Temperature - dTemp);
                            if (Raylib.IsKeyPressed(KeyboardKey.V))
                            {
                                air.MolesO2 = 0; air.MolesN2 = 0; air.Temperature = 2f;
                            }
                        }
                    }

                    // Глобальные клавиши
                    if (Raylib.IsKeyPressed(KeyboardKey.R)) { state = new GameState(256); SpawnTestItems(); }
                    if (Raylib.IsKeyPressed((KeyboardKey)49)) { state = new GameState(10); SpawnTestItems(); }
                    if (Raylib.IsKeyPressed((KeyboardKey)50)) { state = new GameState(100); SpawnTestItems(); }
                    if (Raylib.IsKeyPressed((KeyboardKey)51)) { state = new GameState(256); SpawnTestItems(); }
                    if (Raylib.IsKeyPressed(KeyboardKey.PageUp)) state.TileSize = Math.Min(state.TileSize + 4, 64);
                    if (Raylib.IsKeyPressed(KeyboardKey.PageDown)) state.TileSize = Math.Max(state.TileSize - 4, 8);
                    if (Raylib.IsKeyPressed(KeyboardKey.F))
                    {
                        state.FogOfWar = !state.FogOfWar;
                        if (!state.FogOfWar)
                            for (int x = 0; x < state.Map.Width; x++)
                                for (int y = 0; y < state.Map.Height; y++)
                                    state.Map.Tiles[x, y].IsVisible = true;
                        else
                            VisionSystem.UpdateVisibility(state.Map, state.PlayerX, state.PlayerY, state.ViewRadius);
                    }
                    if (Raylib.IsKeyPressed(KeyboardKey.F11)) Raylib.ToggleFullscreen();
                    if (Raylib.IsKeyPressed(KeyboardKey.L))
                    {
                        if (state.TargetFPS == 60) state.TargetFPS = 144;
                        else if (state.TargetFPS == 144) state.TargetFPS = 0;
                        else state.TargetFPS = 60;
                        Raylib.SetTargetFPS(state.TargetFPS);
                    }
                }

                // Атмосфера тикает всегда
                atmosTimer++;
                if (atmosTimer >= 15)
                {
                    double atmosStart = stopwatch.Elapsed.TotalMilliseconds;
                    state.Linda.Process();
                    lastAtmosTimeMs = stopwatch.Elapsed.TotalMilliseconds - atmosStart;
                    atmosTimer = 0;
                }

                // Рендер (передаём chatFont!)
                double renderStart = stopwatch.Elapsed.TotalMilliseconds;
                RenderSystem.DrawAll(state, wallTex, floorTex, playerTex,
                    lastAtmosTimeMs, lastVisionTimeMs, lastRenderTimeMs, fps, chatFont);
                lastRenderTimeMs = stopwatch.Elapsed.TotalMilliseconds - renderStart;

                frameCount++;
                fpsTimer += Raylib.GetFrameTime();
                if (fpsTimer >= 1.0) { fps = frameCount; frameCount = 0; fpsTimer = 0; }
                lastFrameTimeMs = stopwatch.Elapsed.TotalMilliseconds - frameStart;
            }

            Raylib.UnloadTexture(wallTex);
            Raylib.UnloadTexture(floorTex);
            Raylib.UnloadTexture(playerTex);
            Raylib.UnloadFont(chatFont);
            foreach (var tex in state.ItemTextures.Values) Raylib.UnloadTexture(tex);
            Raylib.CloseWindow();
        }
    }
}