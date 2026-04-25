using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace SS15.Rendering.OpenGL.Graphics;

public sealed class Texture : IDisposable
{
    public int Handle { get; }
    public int Width { get; }
    public int Height { get; }

    internal Texture(int handle, int width, int height)
    {
        Handle = handle;
        Width = width;
        Height = height;
    }

    public static Texture LoadFromFile(string path)
    {
        try
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"Texture not found: {path}");
            using var stream = File.OpenRead(path);
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            int handle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, handle);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            GL.BindTexture(TextureTarget.Texture2D, 0);
            return new Texture(handle, image.Width, image.Height);
        }
        catch
        {
            return CreateFallback(path);
        }
    }

    public static Texture CreateFallback(string path)
    {
        byte r, g, b;
        string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        if (name.Contains("floor")) { r = 128; g = 128; b = 128; }
        else if (name.Contains("wall")) { r = 200; g = 50; b = 50; }
        else if (name.Contains("cat") || name.Contains("mob")) { r = 255; g = 255; b = 0; }
        else { r = 255; g = 0; b = 255; }

        const int size = 2;
        byte[] pixels = new byte[size * size * 4];
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = r; pixels[i + 1] = g; pixels[i + 2] = b; pixels[i + 3] = 255;
        }

        int handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, handle);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
            size, size, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        GL.BindTexture(TextureTarget.Texture2D, 0);
        return new Texture(handle, size, size);
    }

    public void Bind() => GL.BindTexture(TextureTarget.Texture2D, Handle);
    public void Dispose() => GL.DeleteTexture(Handle);
}