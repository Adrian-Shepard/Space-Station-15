using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace SS15.Rendering.OpenGL.Graphics;
// кодер глупый челов
public sealed class DmiState
{
    public string Name { get; set; } = "";
    public int Frames { get; set; }
    public int[] Delays { get; set; } = Array.Empty<int>();
    public Dictionary<int, int> DirFrameOffset { get; set; } = new();
}

public sealed class DmiResult
{
    public Texture Texture { get; set; } = null!;
    public byte[] PixelData { get; set; } = null!;
    public int IconWidth { get; set; }
    public int IconHeight { get; set; }
    public List<DmiState> States { get; set; } = new();
}

public static class DmiLoader
{
    public static DmiResult Load(string path)
    {
        byte[] fileBytes = File.ReadAllBytes(path);

        int pngStart = FindPngStart(fileBytes);
        if (pngStart == -1)
            throw new InvalidDataException("DMI file does not contain a valid PNG image.");

        using var ms = new MemoryStream(fileBytes, pngStart, fileBytes.Length - pngStart);
        ImageResult image = ImageResult.FromStream(ms, ColorComponents.RedGreenBlueAlpha);
        byte[] pixelData = image.Data;

        Texture fullTex = CreateTexture(pixelData, image.Width, image.Height);

        string header = Encoding.ASCII.GetString(fileBytes, 0, pngStart);
        var states = ParseStates(header);

        // Вывод отладочной информации в консоль
        Console.WriteLine($"=== DMI loaded: {Path.GetFileName(path)} ({states.Count} states) ===");
        foreach (var s in states)
            Console.WriteLine($"  State: '{s.Name}' frames={s.Frames} delays={string.Join(",", s.Delays)}");
        Console.WriteLine();

        int iconWidth = 32, iconHeight = 32;
        string? firstLine = GetFirstLine(header);
        if (firstLine != null)
        {
            var eq = firstLine.IndexOf('=');
            if (eq > 0 && firstLine.Substring(0, eq).Trim() == "# DMI")
            {
                string sizePart = firstLine.Substring(eq + 1).Trim();
                var dims = sizePart.Split('x');
                if (dims.Length == 2 && int.TryParse(dims[0], out int w) && int.TryParse(dims[1], out int h))
                {
                    iconWidth = w;
                    iconHeight = h;
                }
            }
        }

        return new DmiResult
        {
            Texture = fullTex,
            PixelData = pixelData,
            IconWidth = iconWidth,
            IconHeight = iconHeight,
            States = states
        };
    }

    private static int FindPngStart(byte[] data)
    {
        byte[] sig = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        for (int i = 0; i <= data.Length - sig.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < sig.Length; j++)
                if (data[i + j] != sig[j]) { match = false; break; }
            if (match) return i;
        }
        return -1;
    }

    private static Texture CreateTexture(byte[] rgba, int w, int h)
    {
        int handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, handle);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
            w, h, 0, PixelFormat.Rgba, PixelType.UnsignedByte, rgba);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        return new Texture(handle, w, h);
    }

    private static List<DmiState> ParseStates(string header)
    {
        var states = new List<DmiState>();
        var lines = header.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        DmiState? current = null;

        void FinishState()
        {
            if (current != null)
            {
                states.Add(current);
                current = null;
            }
        }

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (line.StartsWith("state ", StringComparison.OrdinalIgnoreCase))
            {
                FinishState();
                int eq = line.IndexOf('=');
                if (eq >= 0)
                {
                    string val = line.Substring(eq + 1).Trim();
                    // Убираем кавычки, если есть
                    if (val.StartsWith("\"") && val.EndsWith("\""))
                        val = val.Substring(1, val.Length - 2);
                    if (!string.IsNullOrEmpty(val))
                        current = new DmiState { Name = val };
                }
            }
            else if (current != null)
            {
                if (line.StartsWith("frames ", StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(line.Substring(7).Trim(), out int f);
                    current.Frames = f;
                }
                else if (line.StartsWith("delay ", StringComparison.OrdinalIgnoreCase))
                {
                    string delStr = line.Substring(6).Trim();
                    var parts = delStr.Split(',');
                    current.Delays = new int[parts.Length];
                    for (int i = 0; i < parts.Length; i++)
                        int.TryParse(parts[i].Trim(), out current.Delays[i]);
                }
            }
        }
        FinishState();

        // Вычисляем смещения направлений (1=down, 2=left, 3=up, 4=right)
        foreach (var s in states)
        {
            s.DirFrameOffset.Clear();
            if (s.Frames == 0) s.Frames = 1;
            int framesPerDir = s.Frames / 4;
            if (framesPerDir < 1) framesPerDir = 1;
            for (int d = 0; d < 4; d++)
                s.DirFrameOffset[d + 1] = d * framesPerDir;
        }

        return states;
    }

    private static string? GetFirstLine(string header)
    {
        int end = header.IndexOfAny(new[] { '\r', '\n' });
        return end >= 0 ? header.Substring(0, end) : null;
    }
}