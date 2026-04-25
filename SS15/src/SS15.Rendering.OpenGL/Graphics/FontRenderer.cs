using System;
using System.Collections.Generic;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace SS15.Rendering.OpenGL.Graphics;

public sealed class FontRenderer : IDisposable
{
    private const string VertexShader = @"
#version 330 core
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoord;
out vec2 TexCoord;
uniform mat4 projection;
void main()
{
    gl_Position = projection * vec4(aPos, 0.0, 1.0);
    TexCoord = aTexCoord;
}";

    private const string FragmentShader = @"
#version 330 core
in vec2 TexCoord;
out vec4 FragColor;
uniform sampler2D fontTexture;
uniform vec4 textColor;
void main()
{
    float alpha = texture(fontTexture, TexCoord).r;
    FragColor = vec4(textColor.rgb, textColor.a * alpha);
}";

    private readonly int _vao, _vbo, _ebo;
    private readonly ShaderProgram _shader;
    private readonly Texture _fontAtlas;
    private readonly Dictionary<char, Glyph> _glyphs;
    private readonly int _charsPerRow;

    private const int MaxBatchChars = 1024;
    private readonly float[] _vertices = new float[MaxBatchChars * 16];
    private readonly uint[] _indices = new uint[MaxBatchChars * 6];

    public FontRenderer(string fontImagePath, int charWidth = 8, int charHeight = 8)
    {
        _charsPerRow = 16;

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, MaxBatchChars * 16 * sizeof(float), IntPtr.Zero, BufferUsageHint.StreamDraw);

        for (int i = 0; i < MaxBatchChars; i++)
        {
            uint offset = (uint)(i * 4);
            int idx = i * 6;
            _indices[idx + 0] = offset + 0;
            _indices[idx + 1] = offset + 1;
            _indices[idx + 2] = offset + 2;
            _indices[idx + 3] = offset + 0;
            _indices[idx + 4] = offset + 2;
            _indices[idx + 5] = offset + 3;
        }
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.BindVertexArray(0);

        _shader = new ShaderProgram(VertexShader, FragmentShader);

        _fontAtlas = Texture.LoadFromFile(fontImagePath);
        if (_fontAtlas.Width < 128 || _fontAtlas.Height < 64)
        {
            _fontAtlas.Dispose();
            _fontAtlas = CreateBuiltInFontTexture();
        }

        _glyphs = GenerateGlyphs(charWidth, charHeight);
    }

    private static Texture CreateBuiltInFontTexture()
    {
        const int charW = 8, charH = 8;
        const int cols = 16;
        const int rows = 6;   // 96 символов, 6 строк по 16
        const int texWidth = cols * charW;
        const int texHeight = rows * charH;

        byte[] pixels = new byte[texWidth * texHeight];

        var fontData = BuiltInFont.Data;

        for (int charIndex = 0; charIndex < fontData.Length; charIndex++)
        {
            byte[] charBits = fontData[charIndex];
            int col = charIndex % cols;
            int row = charIndex / cols;

            // Записываем символ в текстуру, переворачивая его по вертикали
            // OpenGL ожидает, что начало текстуры – нижняя строка, поэтому
            // верхняя строка символа должна попасть в нижнюю позицию внутри ячейки.
            for (int srcY = 0; srcY < charH; srcY++)
            {
                byte line = charBits[srcY];
                int destY = row * charH + (charH - 1 - srcY); // переворот по Y
                for (int x = 0; x < charW; x++)
                {
                    int mask = 1 << (7 - x);
                    int px = col * charW + x;
                    pixels[destY * texWidth + px] = (line & mask) != 0 ? (byte)255 : (byte)0;
                }
            }
        }

        int handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, handle);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8,
            texWidth, texHeight, 0, PixelFormat.Red, PixelType.UnsignedByte, pixels);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        GL.BindTexture(TextureTarget.Texture2D, 0);
        return new Texture(handle, texWidth, texHeight);
    }

    private Dictionary<char, Glyph> GenerateGlyphs(int cw, int ch)
    {
        var glyphs = new Dictionary<char, Glyph>();
        float tw = _fontAtlas.Width, th = _fontAtlas.Height;
        float tcx = cw / tw, tcy = ch / th;

        for (char c = (char)32; c <= 126; c++)
        {
            int index = c - 32;
            int col = index % _charsPerRow;
            int row = index / _charsPerRow;
            float u = col * tcx;
            float v = row * tcy;
            glyphs[c] = new Glyph(
                System.Numerics.Vector2.Zero,
                new System.Numerics.Vector2(cw, ch),
                new System.Numerics.Vector2(u, v),
                new System.Numerics.Vector2(u + tcx, v + tcy),
                cw
            );
        }
        return glyphs;
    }

    public void DrawString(Matrix4 projection, string text, OpenTK.Mathematics.Vector2 position, float scale, OpenTK.Mathematics.Vector4 color)
    {
        if (string.IsNullOrEmpty(text)) return;

        int len = Math.Min(text.Length, MaxBatchChars);
        float x = position.X, y = position.Y;

        for (int i = 0; i < len; i++)
        {
            char c = text[i];
            if (!_glyphs.TryGetValue(c, out Glyph glyph))
                c = '?';
            if (!_glyphs.TryGetValue(c, out glyph))
                continue;

            float x0 = x, y0 = y;
            float x1 = x + glyph.Size.X * scale;
            float y1 = y + glyph.Size.Y * scale;

            int vi = i * 16;
           _vertices[vi + 0] = x0; _vertices[vi + 1] = y1;
            _vertices[vi + 2] = glyph.UV1.X; _vertices[vi + 3] = glyph.UV0.Y; // левый верхний → правый UV
            _vertices[vi + 4] = x0; _vertices[vi + 5] = y0;
            _vertices[vi + 6] = glyph.UV1.X; _vertices[vi + 7] = glyph.UV1.Y; // левый нижний → правый UV
            _vertices[vi + 8] = x1; _vertices[vi + 9] = y0;
            _vertices[vi + 10] = glyph.UV0.X; _vertices[vi + 11] = glyph.UV1.Y; // правый нижний → левый UV
            _vertices[vi + 12] = x1; _vertices[vi + 13] = y1;
            _vertices[vi + 14] = glyph.UV0.X; _vertices[vi + 15] = glyph.UV0.Y; // правый верхний → левый UV

            x += glyph.Advance * scale;
        }

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, len * 16 * sizeof(float), _vertices);

        _shader.Use();
        _shader.SetMatrix4("projection", projection);
        _shader.SetVector4("textColor", color);
        GL.ActiveTexture(TextureUnit.Texture0);
        _fontAtlas.Bind();
        _shader.SetInt("fontTexture", 0);

        GL.DrawElements(PrimitiveType.Triangles, len * 6, DrawElementsType.UnsignedInt, IntPtr.Zero);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        _fontAtlas.Dispose();
        _shader.Dispose();
        GL.DeleteBuffer(_vbo);
        GL.DeleteBuffer(_ebo);
        GL.DeleteVertexArray(_vao);
    }
}

internal static class BuiltInFont
{
    public static readonly byte[][] Data = new byte[96][];

    static BuiltInFont()
    {
        for (int i = 0; i < 96; i++) Data[i] = new byte[8];

        byte[][] digits = new byte[][]
        {
            new byte[] {0x3E, 0x63, 0x73, 0x7B, 0x6F, 0x67, 0x3E, 0x00}, // 0
            new byte[] {0x0C, 0x0E, 0x0C, 0x0C, 0x0C, 0x0C, 0x3F, 0x00}, // 1
            new byte[] {0x1E, 0x33, 0x30, 0x1C, 0x06, 0x33, 0x3F, 0x00}, // 2
            new byte[] {0x1E, 0x33, 0x30, 0x1C, 0x30, 0x33, 0x1E, 0x00}, // 3
            new byte[] {0x38, 0x3C, 0x36, 0x33, 0x7F, 0x30, 0x78, 0x00}, // 4
            new byte[] {0x3F, 0x03, 0x1F, 0x30, 0x30, 0x33, 0x1E, 0x00}, // 5
            new byte[] {0x1C, 0x06, 0x03, 0x1F, 0x33, 0x33, 0x1E, 0x00}, // 6
            new byte[] {0x3F, 0x33, 0x30, 0x18, 0x0C, 0x0C, 0x0C, 0x00}, // 7
            new byte[] {0x1E, 0x33, 0x33, 0x1E, 0x33, 0x33, 0x1E, 0x00}, // 8
            new byte[] {0x1E, 0x33, 0x33, 0x3E, 0x30, 0x18, 0x0E, 0x00}  // 9
        };
        for (int i = 0; i < 10; i++) Data[16 + i] = digits[i];

        string[] capLetters = new string[]
        {
            "3E63637F63636363", "3F6363633F6363633F", "3C6603030366033C",
            "1F3663636363361F", "7F4343131F1343437F", "7F4343131F13030303",
            "3C6603037B63633E", "636363637F63636363", "3C1818181818183C",
            "78303030333333FF", "6366361F0F1F366663", "030303030303437F",
            "63777F6B63636363", "63676F7B73636363", "3E6363636363633E",
            "3F6363633F030303", "3E6363636B673E38", "3F6363633F366363",
            "3E63030E3863633E", "7E5A18181818183C", "636363636363633E",
            "6363636363361C08", "6363636B7F776363", "6363361C1C366363",
            "6666663C1818183C", "7F6331184C667F7F"
        };
        for (int i = 0; i < 26; i++)
        {
            string hex = capLetters[i];
            for (int y = 0; y < 8; y++)
                Data[33 + i][y] = Convert.ToByte(hex.Substring(y * 2, 2), 16);
        }

        string[] lowLetters = new string[]
        {
            "00001E303E333E00", "03031F3333331F00", "00001E3303331E00",
            "30303E3333333E00", "00001E333F031E00", "1C360C1E0C0C1E00",
            "00003E33333E303F", "03033F3333333300", "0C000C0C0C0C0E00",
            "180018181818181E", "030333361E366300", "0E0C0C0C0C0C3F00",
            "00006B7F7F6B6300", "00001F3333333300", "00001E3333331E00",
            "00001F33333F0303", "00003E33333E3030", "00001B2E0C0C1E00",
            "00003E031E301F00", "0C0C3E0C0C0C1A00", "0000333333333E00",
            "00003333361C0800", "0000636B7F7F3600", "000063361C366300",
            "00003333333E303F", "00003F231C307F00"
        };
        for (int i = 0; i < 26; i++)
        {
            string hex = lowLetters[i];
            for (int y = 0; y < 8; y++)
                Data[65 + i][y] = Convert.ToByte(hex.Substring(y * 2, 2), 16);
        }

        Data[1] = new byte[] { 0x08, 0x08, 0x08, 0x08, 0x08, 0x00, 0x08, 0x00 };
        Data[2] = new byte[] { 0x14, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        Data[14] = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x00 };
        Data[12] = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x18, 0x0C };
        Data[13] = new byte[] { 0x00, 0x00, 0x00, 0x3E, 0x00, 0x00, 0x00, 0x00 };
        Data[31] = new byte[] { 0x1E, 0x33, 0x30, 0x18, 0x0C, 0x00, 0x0C, 0x00 };
    }
}