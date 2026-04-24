// =============================================================================
// Code/Systems/RenderSystem.cs
// =============================================================================
using Raylib_cs;
using SS15.Code.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace SS15.Code.Systems
{
    public static class RenderSystem
    {
        private const int ScreenWidth = 800;
        private const int ScreenHeight = 600;

        // ── Точка входа ──
        public static void DrawAll(GameState state,
            Texture2D wallTex, Texture2D floorTex, Texture2D playerTex,
            double lastAtmosMs, double lastVisionMs, double lastRenderMs, int fps,
            Font font)
        {
            int camX = state.PlayerX * state.TileSize - ScreenWidth / 2 + state.TileSize / 2;
            int camY = state.PlayerY * state.TileSize - ScreenHeight / 2 + state.TileSize / 2;

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            DrawTiles(state, camX, camY, wallTex, floorTex);
            DrawWorldEntities(state, camX, camY);
            DrawPlayer(state, camX, camY, playerTex);
            DrawClassicUI(state, camX, camY);
            DrawChat(state, font);
            DrawPerformanceInfo(state, lastAtmosMs, lastVisionMs, lastRenderMs, fps);

            if (state.MenuOpen)
                DrawMenu(state);

            Raylib.EndDrawing();
        }

        // ── Проверка, находится ли мышь над любым UI ──
        public static bool IsMouseOverUI()
        {
            Vector2 mouse = Raylib.GetMousePosition();
            // Нижняя панель инвентаря
            if (mouse.Y >= ScreenHeight - 80) return true;
            // Чат
            int chatY = ScreenHeight - 80 - 150 - 10;
            Rectangle chatRect = new Rectangle(10, chatY, 250, 150);
            if (Raylib.CheckCollisionPointRec(mouse, chatRect)) return true;
            return false;
        }

        // ── Обработка клика по слотам рук и карманов ──
        public static void HandleInventoryClick(GameState state)
        {
            if (!Raylib.IsMouseButtonPressed(MouseButton.Left))
                return;

            int panelY = ScreenHeight - 80;
            int iconSize = 48;
            int handsCenterX = ScreenWidth / 2 - 100;

            int leftHandX = handsCenterX;
            Rectangle leftHandRect = new Rectangle(leftHandX, panelY + 15, iconSize, iconSize);

            int rightHandX = leftHandX + iconSize + 20;
            Rectangle rightHandRect = new Rectangle(rightHandX, panelY + 15, iconSize, iconSize);

            int pocketX = rightHandX + iconSize + 30;
            Rectangle pocket1Rect = new Rectangle(pocketX, panelY + 15, iconSize, iconSize);
            Rectangle pocket2Rect = new Rectangle(pocketX + iconSize + 8, panelY + 15, iconSize, iconSize);

            Vector2 mouse = Raylib.GetMousePosition();

            if (Raylib.CheckCollisionPointRec(mouse, leftHandRect))
                HandleHandClick(state, true);
            else if (Raylib.CheckCollisionPointRec(mouse, rightHandRect))
                HandleHandClick(state, false);
            else if (Raylib.CheckCollisionPointRec(mouse, pocket1Rect))
                HandlePocketClick(state, 0);
            else if (Raylib.CheckCollisionPointRec(mouse, pocket2Rect))
                HandlePocketClick(state, 1);
        }

        private static void HandleHandClick(GameState state, bool isLeftHand)
        {
            // Если кликнули по активной руке и в ней есть предмет — ничего
            if (isLeftHand && state.ActiveLeftHand && state.LeftHand != null) return;
            if (!isLeftHand && !state.ActiveLeftHand && state.RightHand != null) return;
            // В остальных случаях меняем руки местами
            InventorySystem.SwapHands(state);
        }

        private static void HandlePocketClick(GameState state, int pocketIndex)
        {
            Entity? activeItem = state.ActiveHandItem;
            if (activeItem != null)
            {
                Entity? pocketItem = pocketIndex == 0 ? state.Pocket1 : state.Pocket2;
                if (pocketItem == null)
                    InventorySystem.MoveFromHandToPocket(state, state.ActiveLeftHand, pocketIndex);
                else
                    InventorySystem.SwapActiveHandWithPocket(state, pocketIndex);
            }
            else
            {
                InventorySystem.MoveFromPocketToActiveHand(state, pocketIndex);
            }
        }

        // ─────────────────── Тайлы ───────────────────
        private static void DrawTiles(GameState state, int camX, int camY, Texture2D wallTex, Texture2D floorTex)
        {
            var map = state.Map;
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    int sx = x * state.TileSize - camX;
                    int sy = y * state.TileSize - camY;
                    if (sx < -state.TileSize || sx > ScreenWidth || sy < -state.TileSize || sy > ScreenHeight)
                        continue;

                    Tile tile = map.Tiles[x, y];
                    bool canSee = !state.FogOfWar || tile.IsVisible;
                    if (!canSee) continue;

                    Rectangle dest = new Rectangle(sx, sy, state.TileSize, state.TileSize);
                    if (tile.IsWall)
                    {
                        Raylib.DrawTexturePro(wallTex,
                            new Rectangle(0, 0, wallTex.Width, wallTex.Height),
                            dest, Vector2.Zero, 0f, Color.White);
                    }
                    else
                    {
                        (byte r, byte g, byte b) = tile.Air.GetVisualColor();
                        Color tint = new Color((byte)r, (byte)g, (byte)b, (byte)255);
                        Raylib.DrawTexturePro(floorTex,
                            new Rectangle(0, 0, floorTex.Width, floorTex.Height),
                            dest, Vector2.Zero, 0f, tint);
                    }
                }
            }
        }

        // ─────────────────── Предметы на полу ───────────────────
        private static void DrawWorldEntities(GameState state, int camX, int camY)
        {
            foreach (var entity in state.WorldEntities)
            {
                var pos = entity.GetComponent<Position>();
                int sx = pos.X * state.TileSize - camX;
                int sy = pos.Y * state.TileSize - camY;
                if (sx < -state.TileSize || sx > ScreenWidth || sy < -state.TileSize || sy > ScreenHeight)
                    continue;
                if (state.FogOfWar && !state.Map.Tiles[pos.X, pos.Y].IsVisible) continue;

                var renderable = entity.GetComponent<Renderable>();
                if (renderable.TexturePath != null)
                {
                    Texture2D tex = GetOrLoadItemTexture(state, renderable.TexturePath);
                    Rectangle dest = new Rectangle(sx, sy, state.TileSize, state.TileSize);
                    Raylib.DrawTexturePro(tex, new Rectangle(0, 0, tex.Width, tex.Height),
                        dest, Vector2.Zero, 0f, Color.White);
                }
                else
                {
                    Raylib.DrawRectangle(sx, sy, state.TileSize, state.TileSize, Color.Blue);
                }
            }
        }

        // ─────────────────── Игрок ───────────────────
        private static void DrawPlayer(GameState state, int camX, int camY, Texture2D playerTex)
        {
            int sx = state.PlayerX * state.TileSize - camX;
            int sy = state.PlayerY * state.TileSize - camY;
            Rectangle dest = new Rectangle(sx, sy, state.TileSize, state.TileSize);
            Raylib.DrawTexturePro(playerTex, new Rectangle(0, 0, playerTex.Width, playerTex.Height),
                dest, Vector2.Zero, 0f, Color.White);
        }

        // ── Классический UI (руки, карманы) ──
        private static void DrawClassicUI(GameState state, int camX, int camY)
        {
            int panelY = ScreenHeight - 80;
            Raylib.DrawRectangle(0, panelY, ScreenWidth, 80, new Color(30, 30, 30, 200));

            int iconSize = 48;
            int handsCenterX = ScreenWidth / 2 - 100;
            Raylib.DrawText("Hands", handsCenterX, panelY - 20, 14, Color.White);

            int leftHandX = handsCenterX;
            Rectangle leftHandRect = new Rectangle(leftHandX, panelY + 15, iconSize, iconSize);
            DrawHandSlot(state, state.LeftHand, leftHandRect, "L", state.ActiveLeftHand);

            int rightHandX = leftHandX + iconSize + 20;
            Rectangle rightHandRect = new Rectangle(rightHandX, panelY + 15, iconSize, iconSize);
            DrawHandSlot(state, state.RightHand, rightHandRect, "R", !state.ActiveLeftHand);

            int pocketX = rightHandX + iconSize + 30;
            Raylib.DrawText("Pockets", pocketX, panelY - 20, 12, Color.White);
            Rectangle pocket1Rect = new Rectangle(pocketX, panelY + 15, iconSize, iconSize);
            Rectangle pocket2Rect = new Rectangle(pocketX + iconSize + 8, panelY + 15, iconSize, iconSize);
            DrawItemSlot(state, state.Pocket1, pocket1Rect);
            DrawItemSlot(state, state.Pocket2, pocket2Rect);

            string hints = "WASD:Move  Z:Use  X:Swap  Q:Drop  LMB:Interact  T:Chat  ESC:Menu";
            Raylib.DrawText(hints, 10, panelY + 65, 10, Color.LightGray);

            // Атмосфера и выделение тайла только если мышь НЕ над UI
            if (!IsMouseOverUI())
            {
                int mouseWorldX = (Raylib.GetMouseX() + camX) / state.TileSize;
                int mouseWorldY = (Raylib.GetMouseY() + camY) / state.TileSize;
                if (mouseWorldX >= 0 && mouseWorldX < state.Map.Width &&
                    mouseWorldY >= 0 && mouseWorldY < state.Map.Height)
                {
                    Tile hoverTile = state.Map.Tiles[mouseWorldX, mouseWorldY];
                    if ((!state.FogOfWar || hoverTile.IsVisible) && !hoverTile.IsWall)
                    {
                        float pressure = hoverTile.Air.Pressure / 1000.0f;
                        float temp = hoverTile.Air.Temperature - 273.15f;
                        string info = $"P: {pressure:F1}  T: {temp:F1}";
                        Raylib.DrawText(info, ScreenWidth - 130, 10, 12, Color.White);
                        int mScreenX = mouseWorldX * state.TileSize - camX;
                        int mScreenY = mouseWorldY * state.TileSize - camY;
                        Raylib.DrawRectangleLines(mScreenX, mScreenY, state.TileSize, state.TileSize, Color.White);
                    }
                }
            }
        }

        private static void DrawHandSlot(GameState state, Entity? item, Rectangle rect, string handLabel, bool isActive)
        {
            Color bgColor = isActive ? new Color(80, 80, 80, 255) : new Color(50, 50, 50, 255);
            Raylib.DrawRectangleRec(rect, bgColor);
            Color borderColor = isActive ? new Color(160, 160, 160, 255) : new Color(80, 80, 80, 255);
            Raylib.DrawRectangleLinesEx(rect, 2, borderColor);

            if (item != null)
            {
                var renderable = item.GetComponent<Renderable>();
                if (renderable.TexturePath != null)
                {
                    Texture2D tex = GetOrLoadItemTexture(state, renderable.TexturePath);
                    Raylib.DrawTexturePro(tex, new Rectangle(0, 0, tex.Width, tex.Height),
                        rect, Vector2.Zero, 0f, Color.White);
                }
                else
                {
                    Raylib.DrawRectangleRec(rect, Color.Gray);
                }
            }

            Color letterColor = new Color(255, 255, 255, 80);
            int fontSize = 20;
            int textX = (int)(rect.X + rect.Width / 2) - Raylib.MeasureText(handLabel, fontSize) / 2;
            int textY = (int)(rect.Y + rect.Height / 2) - fontSize / 2;
            Raylib.DrawText(handLabel, textX, textY, fontSize, letterColor);
        }

        private static void DrawItemSlot(GameState state, Entity? item, Rectangle rect)
        {
            Raylib.DrawRectangleRec(rect, new Color(40, 40, 40, 255));
            Raylib.DrawRectangleLinesEx(rect, 1, Color.DarkGray);

            if (item != null)
            {
                var renderable = item.GetComponent<Renderable>();
                if (renderable.TexturePath != null)
                {
                    Texture2D tex = GetOrLoadItemTexture(state, renderable.TexturePath);
                    Raylib.DrawTexturePro(tex, new Rectangle(0, 0, tex.Width, tex.Height),
                        rect, Vector2.Zero, 0f, Color.White);
                }
                else
                {
                    Raylib.DrawRectangleRec(rect, Color.Gray);
                }
                Raylib.DrawText(item.GetComponent<Item>().Name, (int)rect.X, (int)rect.Y + (int)rect.Height + 2, 10, Color.White);
            }
        }

        // ── Чат со шрифтом ──
        private static void DrawChat(GameState state, Font font)
        {
            int chatWidth = 250, chatHeight = 150;
            int chatX = 10, chatY = ScreenHeight - 80 - chatHeight - 10;
            Raylib.DrawRectangle(chatX, chatY, chatWidth, chatHeight, new Color(0, 0, 0, 180));
            Raylib.DrawRectangleLines(chatX, chatY, chatWidth, chatHeight, Color.Gray);

            float yOffset = chatY + chatHeight - 15;
            float fontSize = 12f;
            float lineSpacing = 14f;
            for (int i = state.ChatMessages.Count - 1; i >= 0; i--)
            {
                var (sender, text) = state.ChatMessages[i];
                string line = $"[{sender}]: {text}";
                yOffset -= lineSpacing;
                if (yOffset < chatY + 5) break;
                Raylib.DrawTextEx(font, line, new Vector2(chatX + 5, yOffset), fontSize, 1f, Color.White);
            }

            if (state.ChatInputActive)
            {
                int inputY = chatY + chatHeight + 5;
                Raylib.DrawRectangle(chatX, inputY, chatWidth, 20, new Color(30, 30, 30, 200));
                string displayText = state.CurrentChatText + ((Raylib.GetTime() % 1.0 < 0.5) ? "_" : "");
                Raylib.DrawTextEx(font, displayText, new Vector2(chatX + 5, inputY + 3), fontSize, 1f, Color.White);
            }
        }

        // ── Меню ──
        private static void DrawMenu(GameState state)
        {
            Raylib.DrawRectangle(0, 0, ScreenWidth, ScreenHeight, new Color(0, 0, 0, 180));
            int menuX = ScreenWidth / 2 - 150, menuY = ScreenHeight / 2 - 130;
            int menuWidth = 300, menuHeight = 260;
            Raylib.DrawRectangle(menuX, menuY, menuWidth, menuHeight, new Color(50, 50, 50, 255));

            Rectangle gameTab = new Rectangle(menuX, menuY - 25, 100, 25);
            Rectangle settingsTab = new Rectangle(menuX + 100, menuY - 25, 100, 25);
            bool gameHover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), gameTab);
            bool settingsHover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), settingsTab);

            Raylib.DrawRectangleRec(gameTab, state.SettingsTab == 0 ? Color.DarkGray : Color.Gray);
            Raylib.DrawRectangleRec(settingsTab, state.SettingsTab == 1 ? Color.DarkGray : Color.Gray);
            Raylib.DrawText("Game", (int)gameTab.X + 10, (int)gameTab.Y + 4, 16, Color.White);
            Raylib.DrawText("Settings", (int)settingsTab.X + 10, (int)settingsTab.Y + 4, 16, Color.White);

            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                Vector2 mouse = Raylib.GetMousePosition();
                if (Raylib.CheckCollisionPointRec(mouse, gameTab)) state.SettingsTab = 0;
                if (Raylib.CheckCollisionPointRec(mouse, settingsTab)) state.SettingsTab = 1;
            }

            if (state.SettingsTab == 0)
            {
                Rectangle resumeBtn = new Rectangle(menuX + 50, menuY + 40, 200, 30);
                Rectangle quitBtn = new Rectangle(menuX + 50, menuY + 90, 200, 30);
                DrawMenuButton(resumeBtn, "Resume", state);
                DrawMenuButton(quitBtn, "Quit", state);
                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    Vector2 mouse = Raylib.GetMousePosition();
                    if (Raylib.CheckCollisionPointRec(mouse, resumeBtn)) state.MenuOpen = false;
                    if (Raylib.CheckCollisionPointRec(mouse, quitBtn)) Environment.Exit(0);
                }
            }
            else
            {
                int y = menuY + 20;
                Raylib.DrawText($"Zoom: {state.TileSize} px", menuX + 20, y, 14, Color.White);
                Rectangle zoomBar = new Rectangle(menuX + 120, y + 2, 100, 16);
                Raylib.DrawRectangleRec(zoomBar, Color.DarkGray);
                float zoomFill = (state.TileSize - 8) / 56f;
                Rectangle zoomFillRect = new Rectangle(zoomBar.X, zoomBar.Y, zoomBar.Width * zoomFill, zoomBar.Height);
                Raylib.DrawRectangleRec(zoomFillRect, Color.SkyBlue);
                if (Raylib.IsMouseButtonDown(MouseButton.Left) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), zoomBar))
                {
                    float mouseX = Raylib.GetMouseX();
                    float frac = (mouseX - zoomBar.X) / zoomBar.Width;
                    state.TileSize = (int)(8 + frac * 56);
                    if (state.TileSize < 8) state.TileSize = 8;
                    if (state.TileSize > 64) state.TileSize = 64;
                }

                y += 30;
                Raylib.DrawText($"FPS Limit: {((state.TargetFPS == 0) ? "Unlimited" : state.TargetFPS.ToString())}", menuX + 20, y, 14, Color.White);
                Rectangle fps30 = new Rectangle(menuX + 20, y + 20, 50, 20);
                Rectangle fps60 = new Rectangle(menuX + 80, y + 20, 50, 20);
                Rectangle fps144 = new Rectangle(menuX + 140, y + 20, 50, 20);
                Rectangle fpsUnl = new Rectangle(menuX + 200, y + 20, 70, 20);
                DrawMenuButton(fps30, "30", state);
                DrawMenuButton(fps60, "60", state);
                DrawMenuButton(fps144, "144", state);
                DrawMenuButton(fpsUnl, "Unlim", state);
                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    Vector2 mouse = Raylib.GetMousePosition();
                    if (Raylib.CheckCollisionPointRec(mouse, fps30)) { state.TargetFPS = 30; Raylib.SetTargetFPS(30); }
                    if (Raylib.CheckCollisionPointRec(mouse, fps60)) { state.TargetFPS = 60; Raylib.SetTargetFPS(60); }
                    if (Raylib.CheckCollisionPointRec(mouse, fps144)) { state.TargetFPS = 144; Raylib.SetTargetFPS(144); }
                    if (Raylib.CheckCollisionPointRec(mouse, fpsUnl)) { state.TargetFPS = 0; Raylib.SetTargetFPS(0); }
                }

                y += 50;
                Raylib.DrawText("Fog of War:", menuX + 20, y, 14, Color.White);
                Rectangle fogCheck = new Rectangle(menuX + 140, y, 20, 20);
                Raylib.DrawRectangleRec(fogCheck, new Color(80, 80, 80, 255));
                if (state.FogOfWar)
                    Raylib.DrawText("X", (int)fogCheck.X + 4, (int)fogCheck.Y - 2, 18, Color.Red);
                if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), fogCheck))
                {
                    state.FogOfWar = !state.FogOfWar;
                }
            }
        }

        private static void DrawMenuButton(Rectangle btn, string text, GameState state)
        {
            Vector2 mouse = Raylib.GetMousePosition();
            bool hover = Raylib.CheckCollisionPointRec(mouse, btn);
            Raylib.DrawRectangleRec(btn, hover ? Color.Gray : Color.DarkGray);
            int textX = (int)(btn.X + btn.Width / 2) - Raylib.MeasureText(text, 18) / 2;
            int textY = (int)(btn.Y + btn.Height / 2) - 9;
            Raylib.DrawText(text, textX, textY, 18, Color.White);
        }

        private static void DrawPerformanceInfo(GameState state, double atmosMs, double visionMs, double renderMs, int fps)
        {
            string fpsText = state.TargetFPS == 0 ? "∞" : state.TargetFPS.ToString();
            string info = $"FPS: {fps:00} (L: {fpsText}) | Render: {renderMs:F1}ms";
            info += $"\nAtmos: {atmosMs:F1}ms  Vision: {visionMs:F1}ms";
            Raylib.DrawText(info, ScreenWidth - 230, ScreenHeight - 40, 12, Color.Lime);
        }

        private static Texture2D GetOrLoadItemTexture(GameState state, string fileName)
        {
            if (!state.ItemTextures.ContainsKey(fileName))
            {
                string path = $"Assets/Textures/items/{fileName}";
                if (File.Exists(path))
                    state.ItemTextures[fileName] = Raylib.LoadTexture(path);
                else
                {
                    Image img = Raylib.GenImageColor(16, 16, Color.Gray);
                    Texture2D tex = Raylib.LoadTextureFromImage(img);
                    Raylib.UnloadImage(img);
                    state.ItemTextures[fileName] = tex;
                }
            }
            return state.ItemTextures[fileName];
        }
    }
}