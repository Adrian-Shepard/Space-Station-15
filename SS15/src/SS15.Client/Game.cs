using System;
using System.Collections.Generic;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SS15.Common.World;
using SS15.Rendering.OpenGL.Graphics;
using SS15.Game.Atmos;
// Было using SS15.Atmospherics так как я сделал модульность атмоса, но потом я решил, что это слишком много для такого маленького проекта и просто перенёс всё в SS15.Game.Atmos, так что теперь так
using SS15.Game.Mobs;
using SS15.Game.Objects;
// нахуй я сюда полез 
// ядерный код не трогать или всем пиздец будет, я предупреждал
// ну а если серьезно, то тут просто основной класс игры, который создаёт окно, рендерит карту и спрайты, обрабатывает ввод и т.д.
// он получился довольно большим, но я не вижу смысла дробить его на части, так как это всё тесно связано и не слишком сложное
// в будущем, если игра будет развиваться, можно будет выделить отдельные классы для UI, камеры, атмоса и т.д., но пока что всё в одном файле для простоты
namespace SS15.Client;
// рандомный текст увыыыыы, чтобы не было скучно смотреть на код
public sealed class Game : GameWindow
{
    private TileMapRenderer _tileRenderer = null!;
    private SpriteRenderer _spriteRenderer = null!;
    private FontRenderer _font = null!;
    private Map _map = null!;
    private Entity _player = null!;
    private Matrix4 _proj;

    private OpenTK.Mathematics.Vector2 _camPos = OpenTK.Mathematics.Vector2.Zero;
    private const float CamSmooth = 12f;
    private const int TileSize = 32, PlayerSize = 32;

    private int MapPixelW => _map.Width * TileSize;
    private int MapPixelH => _map.Height * TileSize;

    private float _zoom = 2f;
    private const float ZoomMin = 0.5f, ZoomMax = 4f, ZoomStep = 0.25f;

    // Направление (1=down, 2=left, 3=up, 4=right)
    private int _dir = 1;
    private static readonly string[] DirSuffix = { "", "down", "left", "up", "right" };

    // Чат
    private string? _chatMsg;
    private double _chatTimer;
    private readonly List<string> _chatHistory = new();
    private const int MaxChatHistory = 10;
    private bool _typing;
    private string _inputBuf = "";

    // Меню
    private bool _menuOpen;
    private int _menuBgVao;

    // Атмос
    private AtmosMap _atmos = null!;

    // FPS
    private double _fpsTimer;
    private int _frameCnt;
    private double _fps, _frameMs;

    public Game() : base(GameWindowSettings.Default, new NativeWindowSettings
    {
        ClientSize = new Vector2i(1024, 768),
        Title = "SS15 – Diagonal + Cat Visible",
        APIVersion = new Version(3, 3),
        Profile = ContextProfile.Core,
        WindowBorder = WindowBorder.Fixed,
        NumberOfSamples = 0
    }) { }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ClearColor(0.05f, 0.05f, 0.05f, 1f);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _font = new FontRenderer("Fonts/font");

        // Карта
        _map = new Map(50, 50);
        for (int y = 0; y < 50; y++)
        for (int x = 0; x < 50; x++)
            _map.SetTile(x, y, new TileDef(TileType.Floor, 0));

        _map.SetTile(10, 10, new TileDef(TileType.Wall, 0));
        _map.SetTile(11, 10, new TileDef(TileType.Wall, 0));
        _map.SetTile(20, 15, new TileDef(TileType.Wall, 0));
        _map.SetTile(20, 16, new TileDef(TileType.Wall, 0));
        _map.SetTile(20, 17, new TileDef(TileType.Wall, 0));

        _tileRenderer = new TileMapRenderer(_map, TilePrototypes.ToDictionary());

        // Игрок: используем DMI путь с состоянием и направлением
        var catProto = new CatMob();
        _player = catProto.CreateEntity(1);
        _player.Position = new System.Numerics.Vector2(5 * TileSize, 5 * TileSize);
        _player.SpriteKey = "cat_down";

        var spritePaths = new Dictionary<string, string>();
        for (int d = 1; d <= 4; d++)
        {
            string key = $"cat_{DirSuffix[d]}";
            // Ключ: путь к DMI + состояние + направление
            spritePaths[key] = $"{catProto.DmiPath}:{catProto.StateName}:{d}:0";
        }
        _spriteRenderer = new SpriteRenderer(spritePaths);

        // Атмос
        _atmos = new AtmosMap(50, 50);
        for (int y = 0; y < 50; y++)
        for (int x = 0; x < 50; x++)
        {
            var mix = _atmos.GetCell(x, y);
            mix.SetMoles(GasType.Oxygen, 0.02f);
            mix.SetMoles(GasType.Nitrogen, 0.08f);
            // СО2 нет так как это тестовая площадка с идеалными условиями, а не космос :)
        }
        for (int y = 0; y < 50; y++)
        for (int x = 0; x < 50; x++)
            if (_map.IsTileSolid(x, y))
                _atmos.SetBlocked(x, y, true);

        CreateMenuBackground();
        CenterCamera();
    }

    private void CreateMenuBackground()
    {
        float[] verts =
        {
            -1,  1, 0, 0,
            -1, -1, 0, 1,
             1, -1, 1, 1,
            -1,  1, 0, 0,
             1, -1, 1, 1,
             1,  1, 1, 0
        };

        _menuBgVao = GL.GenVertexArray();
        int vbo = GL.GenBuffer();
        GL.BindVertexArray(_menuBgVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.BindVertexArray(0);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        double dt = args.Time;
        _frameCnt++;
        _fpsTimer += dt;
        if (_fpsTimer >= 0.5)
        {
            _fps = _frameCnt / _fpsTimer;
            _frameMs = _fpsTimer / _frameCnt * 1000.0;
            _frameCnt = 0;
            _fpsTimer = 0;
        }

        _atmos.Simulate((float)dt);
        HandleInput(dt);
        if (!_menuOpen && !_typing) HandleMovement(dt);
        UpdateCamera(dt);

        if (_chatMsg != null)
        {
            _chatTimer -= dt;
            if (_chatTimer <= 0) _chatMsg = null;
        }
    }

    private void HandleInput(double dt)
    {
        var kb = KeyboardState;
        if (kb.IsKeyPressed(Keys.Escape))
        {
            if (_typing) { _typing = false; _inputBuf = ""; return; }
            _menuOpen = !_menuOpen;
        }

        if (_menuOpen)
        {
            if (kb.IsKeyPressed(Keys.Equal) || kb.IsKeyPressed(Keys.KeyPadAdd))
                _zoom = MathHelper.Clamp(_zoom + ZoomStep, ZoomMin, ZoomMax);
            if (kb.IsKeyPressed(Keys.Minus) || kb.IsKeyPressed(Keys.KeyPadSubtract))
                _zoom = MathHelper.Clamp(_zoom - ZoomStep, ZoomMin, ZoomMax);
            return;
        }

        if (!_typing && kb.IsKeyPressed(Keys.T))
        {
            _typing = true;
            _inputBuf = "";
        }

        if (!_typing)
        {
            int tx = (int)(_player.Position.X / TileSize);
            int ty = (int)(_player.Position.Y / TileSize);
            if (_map.IsInBounds(tx, ty))
            {
                var mix = _atmos.GetCell(tx, ty);
                if (kb.IsKeyPressed(Keys.J)) mix.Temperature = Math.Max(0, mix.Temperature - 10f);
                if (kb.IsKeyPressed(Keys.K)) mix.Temperature += 10f;
                if (kb.IsKeyPressed(Keys.O)) mix.SetMoles(GasType.Oxygen, mix.GetMoles(GasType.Oxygen) + 5f);
                if (kb.IsKeyPressed(Keys.I)) mix.SetMoles(GasType.Nitrogen, mix.GetMoles(GasType.Nitrogen) + 5f);
                if (kb.IsKeyPressed(Keys.U)) mix.SetMoles(GasType.CarbonDioxide, mix.GetMoles(GasType.CarbonDioxide) + 5f);
            }
        }

        if (_typing && kb.IsKeyPressed(Keys.Enter))
        {
            if (!string.IsNullOrWhiteSpace(_inputBuf))
            {
                string msg = $"{_player.Name}: {_inputBuf}";
                _chatHistory.Add(msg);
                if (_chatHistory.Count > MaxChatHistory) _chatHistory.RemoveAt(0);
                _chatMsg = _inputBuf;
                _chatTimer = 3.0;
            }
            _typing = false;
            _inputBuf = "";
        }
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (_typing && !char.IsControl(e.AsString[0]))
            _inputBuf += e.AsString;
    }

    private void HandleMovement(double dt)
    {
        var kb = KeyboardState;
        float spd = 150f * (float)dt;
        System.Numerics.Vector2 pos = _player.Position;

        // Определяем нажатые направления независимо
        bool left  = kb.IsKeyDown(Keys.Left)  || kb.IsKeyDown(Keys.A);
        bool right = kb.IsKeyDown(Keys.Right) || kb.IsKeyDown(Keys.D);
        bool up    = kb.IsKeyDown(Keys.Up)    || kb.IsKeyDown(Keys.W);
        bool down  = kb.IsKeyDown(Keys.Down)  || kb.IsKeyDown(Keys.S);

        // Двигаемся по X
        float nx = pos.X;
        if (left) nx -= spd;
        if (right) nx += spd;
        if (!BoxCollides(nx, pos.Y, PlayerSize, PlayerSize)) pos.X = nx;

        // Двигаемся по Y
        float ny = pos.Y;
        if (up) ny -= spd;
        if (down) ny += spd;
        if (!BoxCollides(pos.X, ny, PlayerSize, PlayerSize)) pos.Y = ny;

        _player.Position = pos;

        // Определяем приоритетное направление для спрайта (последняя нажатая клавиша)
        if (left && !right) _dir = 2;
        else if (right && !left) _dir = 4;
        else if (up && !down) _dir = 3;
        else if (down && !up) _dir = 1;
        // Если ничего не нажато, направление не меняется

        if (left || right || up || down)
        {
            _player.SpriteKey = $"cat_{DirSuffix[_dir]}";
            Console.WriteLine($"[Game] SpriteKey set to: '{_player.SpriteKey}'");
        }
    }

    private void UpdateCamera(double dt)
    {
        float vw = ClientSize.X / _zoom, vh = ClientSize.Y / _zoom;
        var target = new OpenTK.Mathematics.Vector2(
            _player.Position.X + PlayerSize / 2f - vw / 2f,
            _player.Position.Y + PlayerSize / 2f - vh / 2f);
        float s = 1f - MathF.Exp(-CamSmooth * (float)dt);
        _camPos = OpenTK.Mathematics.Vector2.Lerp(_camPos, target, s);
        _camPos.X = MathHelper.Clamp(_camPos.X, 0, MapPixelW - vw);
        _camPos.Y = MathHelper.Clamp(_camPos.Y, 0, MapPixelH - vh);
        _proj = Matrix4.CreateOrthographicOffCenter(
            _camPos.X, _camPos.X + vw, _camPos.Y + vh, _camPos.Y, -1, 1);
    }

    private void CenterCamera()
    {
        float vw = ClientSize.X / _zoom, vh = ClientSize.Y / _zoom;
        var target = new OpenTK.Mathematics.Vector2(
            _player.Position.X + PlayerSize / 2f - vw / 2f,
            _player.Position.Y + PlayerSize / 2f - vh / 2f);
        _camPos = new OpenTK.Mathematics.Vector2(
            MathHelper.Clamp(target.X, 0, MapPixelW - vw),
            MathHelper.Clamp(target.Y, 0, MapPixelH - vh));
        _proj = Matrix4.CreateOrthographicOffCenter(
            _camPos.X, _camPos.X + vw, _camPos.Y + vh, _camPos.Y, -1, 1);
    }

    private bool BoxCollides(float x, float y, float w, float h)
    {
        int l = (int)(x / TileSize), r = (int)((x + w - 0.01f) / TileSize);
        int t = (int)(y / TileSize), b = (int)((y + h - 0.01f) / TileSize);
        for (int ty = t; ty <= b; ty++)
        for (int tx = l; tx <= r; tx++)
            if (_map.IsTileSolid(tx, ty)) return true;
        return false;
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        _tileRenderer.Draw(_proj);
        _spriteRenderer.Draw(_proj, new[] { _player });

        // ====== UI ======
        // FPS (тёмно-зелёный)
        _font.DrawString(_proj, $"FPS: {_fps:F1}  {_frameMs:F2}ms",
            new OpenTK.Mathematics.Vector2(_camPos.X + 10, _camPos.Y + 20),
            0.5f, new OpenTK.Mathematics.Vector4(0.1f, 0.5f, 0.1f, 1f));

        // Атмосфера под мышью (тёмно-серый текст)
        var ms = MouseState;
        float worldMx = _camPos.X + ms.X / _zoom;
        float worldMy = _camPos.Y + ms.Y / _zoom;
        int ttx = (int)(worldMx / TileSize), tty = (int)(worldMy / TileSize);
        if (_map.IsInBounds(ttx, tty))
        {
            var mix = _atmos.GetCell(ttx, tty);
            float ax = _camPos.X + ClientSize.X / _zoom - 180;
            float ay = _camPos.Y + 20;
            var dark = new OpenTK.Mathematics.Vector4(0.2f, 0.2f, 0.2f, 1f);
            var mid = new OpenTK.Mathematics.Vector4(0.4f, 0.4f, 0.4f, 1f);
            _font.DrawString(_proj, $"Tile ({ttx},{tty})", new OpenTK.Mathematics.Vector2(ax, ay), 0.5f, mid);
            _font.DrawString(_proj, $"P: {mix.Pressure:F1} kPa", new OpenTK.Mathematics.Vector2(ax, ay + 12), 0.5f, dark);
            _font.DrawString(_proj, $"T: {mix.Temperature:F1} K", new OpenTK.Mathematics.Vector2(ax, ay + 24), 0.5f, dark);
            _font.DrawString(_proj, $"O2:{mix.GetMoles(GasType.Oxygen):F1} N2:{mix.GetMoles(GasType.Nitrogen):F1} CO2:{mix.GetMoles(GasType.CarbonDioxide):F1}",
                new OpenTK.Mathematics.Vector2(ax, ay + 36), 0.5f, dark);
            _font.DrawString(_proj, "[J/K]T [O]O2 [I]N2 [U]CO2",
                new OpenTK.Mathematics.Vector2(ax, ay + 54), 0.4f, dark);
        }

        // Рунчат (тёмно-серый)
        if (_chatMsg != null)
        {
            var rpos = new OpenTK.Mathematics.Vector2(
                _player.Position.X + PlayerSize / 2f - (_chatMsg.Length * 4f),
                _player.Position.Y - 14f);
            _font.DrawString(_proj, _chatMsg, rpos, 0.5f,
                new OpenTK.Mathematics.Vector4(0.2f, 0.2f, 0.2f, 1f));
        }

        // Чат-бокс (тёмно-серый)
        float chatY = _camPos.Y + ClientSize.Y / _zoom - 20f;
        for (int i = 0; i < _chatHistory.Count; i++)
        {
            string line = _chatHistory[_chatHistory.Count - 1 - i];
            var cpos = new OpenTK.Mathematics.Vector2(_camPos.X + 10f, chatY - i * 14);
            _font.DrawString(_proj, line, cpos, 0.5f,
                new OpenTK.Mathematics.Vector4(0.2f, 0.2f, 0.2f, 1f));
        }

        // Строка ввода (тёмно-зелёный)
        if (_typing)
        {
            string prompt = "Say: " + _inputBuf + "_";
            var tpos = new OpenTK.Mathematics.Vector2(_camPos.X + 10f, chatY - _chatHistory.Count * 14 - 20f);
            _font.DrawString(_proj, prompt, tpos, 0.5f,
                new OpenTK.Mathematics.Vector4(0.1f, 0.5f, 0.1f, 1f));
        }

        // Меню с затемнением
        if (_menuOpen)
        {
            var bgShader = new ShaderProgram(BgVertexShader, BgFragmentShader);
            bgShader.Use();
            GL.BindVertexArray(_menuBgVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.BindVertexArray(0);

            float cx = _camPos.X + ClientSize.X / _zoom / 2f;
            float cy = _camPos.Y + ClientSize.Y / _zoom / 2f;
            _font.DrawString(_proj, "-- MENU --", new OpenTK.Mathematics.Vector2(cx - 40f, cy - 30f), 0.8f, new OpenTK.Mathematics.Vector4(1, 1, 0, 1));
            _font.DrawString(_proj, $"Zoom: {_zoom:F2}  (+ / - to adjust)", new OpenTK.Mathematics.Vector2(cx - 60f, cy), 0.5f, new OpenTK.Mathematics.Vector4(1, 1, 1, 1));
            _font.DrawString(_proj, "Press ESC to close", new OpenTK.Mathematics.Vector2(cx - 50f, cy + 20f), 0.5f, new OpenTK.Mathematics.Vector4(0.7f, 0.7f, 0.7f, 1));
        }

        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnUnload()
    {
        _tileRenderer?.Dispose();
        _spriteRenderer?.Dispose();
        _font?.Dispose();
        GL.DeleteVertexArray(_menuBgVao);
        base.OnUnload();
    }

    private const string BgVertexShader = @"
#version 330 core
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoord;
out vec2 TexCoord;
void main()
{
    gl_Position = vec4(aPos, 0.0, 1.0);
    TexCoord = aTexCoord;
}";
    private const string BgFragmentShader = @"
#version 330 core
in vec2 TexCoord;
out vec4 FragColor;
void main()
{
    FragColor = vec4(0.0, 0.0, 0.0, 0.5);
}";
}
//увыыыыы
// ерп ивент был, но он был настолько скучным, что я даже не помню, что там было, и решил не включать его в код