using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SS15.Common.World;

namespace SS15.Rendering.OpenGL.Graphics;

public sealed class TileMapRenderer : IDisposable
{
    private readonly ShaderProgram _shader;
    private int _quadVao, _instanceVbo;
    private readonly Dictionary<TileType, Texture> _textures = new();
    private readonly Map _map;
    private readonly float[] _instanceData;
    private const int TileSize = 32;

    public TileMapRenderer(Map map, Dictionary<TileType, string> textureKeys)
    {
        _map = map;
        foreach (var kv in textureKeys)
            _textures[kv.Key] = ResourceManager.GetTexture(kv.Value);

        _shader = new ShaderProgram(VertexShader, FragmentShader);
        _instanceData = new float[map.Width * map.Height * 4];
        CreateQuadVao();
    }

    private void CreateQuadVao()
    {
        float[] quad = { 0,0,0,0, 1,0,1,0, 1,1,1,1, 0,0,0,0, 1,1,1,1, 0,1,0,1 };
        _quadVao = GL.GenVertexArray();
        int vbo = GL.GenBuffer();
        GL.BindVertexArray(_quadVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, quad.Length * sizeof(float), quad, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        _instanceVbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, _instanceData.Length * sizeof(float), IntPtr.Zero, BufferUsageHint.StreamDraw);

        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribDivisor(2, 1);
        GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        GL.EnableVertexAttribArray(3);
        GL.VertexAttribDivisor(3, 1);

        GL.BindVertexArray(0);
    }

    public void Draw(Matrix4 projection)
    {
        RenderLayer(TileType.Floor, projection);
        RenderLayer(TileType.Wall, projection);
    }

    private void RenderLayer(TileType type, Matrix4 proj)
    {
        if (!_textures.TryGetValue(type, out var tex)) return;

        int count = 0;
        for (int y = 0; y < _map.Height; y++)
        for (int x = 0; x < _map.Width; x++)
        {
            if (_map.GetTile(x, y).Type != type) continue;
            int idx = count * 4;
            _instanceData[idx] = x * TileSize;
            _instanceData[idx+1] = y * TileSize;
            _instanceData[idx+2] = 0;
            _instanceData[idx+3] = 0;
            count++;
        }

        if (count == 0) return;

        GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVbo);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, count * 4 * sizeof(float), _instanceData);

        _shader.Use();
        _shader.SetMatrix4("projection", proj);
        tex.Bind();
        _shader.SetInt("tileTexture", 0);

        GL.BindVertexArray(_quadVao);
        GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, count);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        _shader.Dispose();
        foreach (var t in _textures.Values) t.Dispose();
        GL.DeleteVertexArray(_quadVao);
        GL.DeleteBuffer(_instanceVbo);
    }

    private const string VertexShader = @"
#version 330 core
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec2 aTileOffset;
layout (location = 3) in vec2 aUvVariant;
out vec2 TexCoord;
uniform mat4 projection;
void main() {
    vec2 pos = aPos * 32.0 + aTileOffset;
    gl_Position = projection * vec4(pos, 0.0, 1.0);
    TexCoord = aTexCoord + aUvVariant;
}";

    private const string FragmentShader = @"
#version 330 core
in vec2 TexCoord;
out vec4 FragColor;
uniform sampler2D tileTexture;
void main() {
    FragColor = texture(tileTexture, TexCoord);
}";
}