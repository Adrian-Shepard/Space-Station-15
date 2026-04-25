using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL4;

namespace SS15.Rendering.OpenGL.Graphics;

public static class ResourceManager
{
    private static readonly string[] SearchPaths = {
        "Assets/Textures", "Assets/Sprites", "Assets/Fonts", "Assets/Audio", ""
    };

    private static readonly Dictionary<string, Texture> TextureCache = new();
    private static readonly Dictionary<string, DmiResult> DmiCache = new();

    /// <summary>
    /// Загружает текстуру по ключу. Формат ключа:
    ///   "путь"                     – PNG или первый кадр DMI
    ///   "путь:состояние"           – конкретное состояние DMI (первый кадр, первое направление)
    ///   "путь:состояние:напр"      – конкретное направление (1=down, 2=left, 3=up, 4=right)
    ///   "путь:состояние:напр:кадр" – точный кадр в направлении
    /// Также поддерживаются прямые относительные пути (например "../../Assets/Sprites/Mobs/cat_calico").
    /// </summary>
    public static Texture GetTexture(string key)
    {
        string normalized = Normalize(key);
        if (TextureCache.TryGetValue(normalized, out var cached))
            return cached;

        // Разбираем ключ: путь, состояние, направление, кадр
        string path = key;
        string? state = null;
        int dir = 0, frame = 0;

        int colon1 = key.IndexOf(':');
        if (colon1 >= 0)
        {
            path = key.Substring(0, colon1);
            string rest = key.Substring(colon1 + 1);
            int colon2 = rest.IndexOf(':');
            if (colon2 >= 0)
            {
                state = rest.Substring(0, colon2);
                string dirStr = rest.Substring(colon2 + 1);
                int colon3 = dirStr.IndexOf(':');
                if (colon3 >= 0)
                {
                    dir = int.Parse(dirStr.Substring(0, colon3));
                    frame = int.Parse(dirStr.Substring(colon3 + 1));
                }
                else
                {
                    dir = int.Parse(dirStr);
                }
            }
            else
            {
                state = rest;
            }
        }

        // Сначала пробуем загрузить PNG
        string? pngPath = LocateFile(path, ".png");
        if (pngPath != null)
        {
            Texture tex = Texture.LoadFromFile(pngPath);
            TextureCache[normalized] = tex;
            return tex;
        }

        // Иначе работаем с DMI
        DmiResult? dmi = GetDmiInternal(path);
        if (dmi == null)
        {
            // Если ничего не нашли – цветная заглушка
            Texture fallback = Texture.CreateFallback(key);
            TextureCache[normalized] = fallback;
            return fallback;
        }

        // Извлекаем нужный кадр
        Texture frameTex = ExtractFrame(dmi, state, dir, frame);
        TextureCache[normalized] = frameTex;
        return frameTex;
    }

    /// <summary>
    /// Загружает полный DMI-файл (без извлечения кадров).
    /// </summary>
    public static DmiResult GetDmi(string key)
    {
        string normalized = Normalize(key);
        if (DmiCache.TryGetValue(normalized, out var cached))
            return cached;

        return GetDmiInternal(key)
            ?? throw new FileNotFoundException($"DMI file not found: {key}");
    }

    /// <summary>
    /// Внутренний метод: загружает и кэширует DMI.
    /// </summary>
    private static DmiResult? GetDmiInternal(string key)
    {
        string normalized = Normalize(key);
        if (DmiCache.TryGetValue(normalized, out var cached))
            return cached;

        string? dmiPath = LocateFile(key, ".dmi");
        if (dmiPath == null)
            return null;

        DmiResult dmi = DmiLoader.Load(dmiPath);
        DmiCache[normalized] = dmi;
        return dmi;
    }

    /// <summary>
    /// Вырезает из DMI текстуру нужного кадра.
    /// </summary>
    private static Texture ExtractFrame(DmiResult dmi, string? stateName, int dir, int frame)
    {
        int iconW = dmi.IconWidth;
        int iconH = dmi.IconHeight;
        int cols = dmi.Texture.Width / iconW;

        int frameIndex = 0;

        // Если указано состояние, пытаемся найти его
        DmiState? state = null;
        if (!string.IsNullOrEmpty(stateName))
        {
            state = dmi.States.Find(s =>
                s.Name.Equals(stateName, StringComparison.OrdinalIgnoreCase));
        }

        // Если состояние не найдено (или не указано), берём первый попавшийся кадр
        if (state == null && dmi.States.Count > 0)
            state = dmi.States[0];

        if (state != null)
        {
            if (dir > 0 && state.DirFrameOffset.TryGetValue(dir, out int offset))
                frameIndex = offset + (frame % (state.Frames / 4 > 0 ? state.Frames / 4 : 1));
            else
                frameIndex = frame % state.Frames;
        }

        // Вычисляем позицию кадра в атласе (снизу вверх для OpenGL)
        int col = frameIndex % cols;
        int row = frameIndex / cols;
        int srcX = col * iconW;
        int srcY = row * iconH;

        byte[] framePixels = new byte[iconW * iconH * 4];
        byte[] srcPixels = dmi.PixelData;
        int srcWidth = dmi.Texture.Width;

        // Копируем с инверсией по Y (DMI сверху = OpenGL снизу)
        for (int y = 0; y < iconH; y++)
        {
            int srcYPos = srcY + (iconH - 1 - y);  // Инвертируем Y
            for (int x = 0; x < iconW; x++)
            {
                int srcIndex = ((srcYPos * srcWidth) + (srcX + x)) * 4;
                int destIndex = (y * iconW + x) * 4;
                framePixels[destIndex]     = srcPixels[srcIndex];
                framePixels[destIndex + 1] = srcPixels[srcIndex + 1];
                framePixels[destIndex + 2] = srcPixels[srcIndex + 2];
                framePixels[destIndex + 3] = srcPixels[srcIndex + 3];
            }
        }

        int handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, handle);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
            iconW, iconH, 0, PixelFormat.Rgba, PixelType.UnsignedByte, framePixels);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        return new Texture(handle, iconW, iconH);
    }

    /// <summary>Очищает весь кэш текстур и DMI.</summary>
    public static void ClearCache()
    {
        foreach (var t in TextureCache.Values) t.Dispose();
        foreach (var d in DmiCache.Values) d.Texture.Dispose();
        TextureCache.Clear();
        DmiCache.Clear();
    }

    private static string Normalize(string path) =>
        path.Replace('\\', '/').ToLowerInvariant().TrimEnd('/');

    /// <summary>
    /// Ищет файл с указанным расширением. Сначала проверяет прямой путь (как есть + расширение),
    /// затем обходит папки SearchPaths.
    /// </summary>
    private static string? LocateFile(string key, string extension)
    {
        // Обрезаем часть после : (состояние, направление, кадр)
        int colonPos = key.IndexOf(':');
        string clean = colonPos >= 0 ? key[..colonPos] : key;

        // Убираем расширение если есть
        if (clean.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            clean = clean[..^extension.Length];

        // Прямой путь (с добавленным расширением)
        string direct = clean + extension;
        if (File.Exists(direct))
            return direct;

        // Поиск в папках (Assets/Textures, Assets/Sprites, Assets/Fonts, Assets/Audio, корень)
        foreach (string basePath in SearchPaths)
        {
            string candidate = Path.Combine(basePath, clean + extension);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }
}