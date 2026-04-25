using System;
using System.Collections.Generic;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SS15.Common.World;

namespace SS15.Rendering.OpenGL.Graphics;

public sealed class SpriteRenderer : IDisposable
{
    private readonly ShaderProgram _shader;
    private readonly int _vao, _vbo, _ebo;
    private readonly Dictionary<string, Texture> _textures;
    private readonly float[] _vertices;
    private readonly uint[] _indices;
    private const int MaxSprites = 256;

    public SpriteRenderer(Dictionary<string, string> texturePaths)
    {
        _textures = new Dictionary<string, Texture>();
        foreach (var kvp in texturePaths)
            _textures[kvp.Key] = ResourceManager.GetTexture(kvp.Value);

        _shader = new ShaderProgram(VertexShader, FragmentShader);

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();

        _vertices = new float[MaxSprites * 16];
        _indices = new uint[MaxSprites * 6];
        for (int i = 0; i < MaxSprites; i++)
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

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), IntPtr.Zero, BufferUsageHint.StreamDraw);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.BindVertexArray(0);
    }

    public void Draw(Matrix4 projection, IEnumerable<Entity> entities)
    {
        int count = 0;
        Texture? currentTexture = null;

        foreach (Entity ent in entities)
        {
            if (!_textures.TryGetValue(ent.SpriteKey, out Texture? tex) || tex == null)
            {
                Console.WriteLine($"[SpriteRenderer] Missing texture for key: '{ent.SpriteKey}'");
                continue;
            }

            if (currentTexture != tex)
            {
                Flush(count);
                count = 0;
                currentTexture = tex;
            }

            if (count >= MaxSprites)
                Flush(count);

            System.Numerics.Vector2 pos = ent.Position;
            float w = 32, h = 32;
            int vi = count * 16;
            _vertices[vi + 0] = pos.X;
            _vertices[vi + 1] = pos.Y + h;
            _vertices[vi + 2] = 0f; _vertices[vi + 3] = 0f;
            _vertices[vi + 4] = pos.X;
            _vertices[vi + 5] = pos.Y;
            _vertices[vi + 6] = 0f; _vertices[vi + 7] = 1f;
            _vertices[vi + 8] = pos.X + w;
            _vertices[vi + 9] = pos.Y;
            _vertices[vi + 10] = 1f; _vertices[vi + 11] = 1f;
            _vertices[vi + 12] = pos.X + w;
            _vertices[vi + 13] = pos.Y + h;
            _vertices[vi + 14] = 1f; _vertices[vi + 15] = 0f;
            count++;
        }
        Flush(count);

        void Flush(int num)
        {
            if (num == 0 || currentTexture == null) return;
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, num * 16 * sizeof(float), _vertices);
            _shader.Use();
            _shader.SetMatrix4("projection", projection);
            currentTexture.Bind();
            _shader.SetInt("spriteTexture", 0);
            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, num * 6, DrawElementsType.UnsignedInt, IntPtr.Zero);
        }
    }

    public void Dispose()
    {
        _shader.Dispose();
        foreach (var tex in _textures.Values)
            tex.Dispose();
        GL.DeleteVertexArray(_vao);
        GL.DeleteBuffer(_vbo);
        GL.DeleteBuffer(_ebo);
    }

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
uniform sampler2D spriteTexture;
void main()
{
    FragColor = texture(spriteTexture, TexCoord);
}";
}