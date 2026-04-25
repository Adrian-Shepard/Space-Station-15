using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace SS15.Rendering.OpenGL.Graphics;

public sealed class ShaderProgram : IDisposable
{
    private readonly int _handle;
    private readonly Dictionary<string, int> _uniformLocations = new();

    public ShaderProgram(string vertexSource, string fragmentSource)
    {
        int vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
        int fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

        _handle = GL.CreateProgram();
        GL.AttachShader(_handle, vertexShader);
        GL.AttachShader(_handle, fragmentShader);
        GL.LinkProgram(_handle);

        GL.GetProgram(_handle, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0)
            throw new Exception($"Shader link error: {GL.GetProgramInfoLog(_handle)}");

        GL.DetachShader(_handle, vertexShader);
        GL.DetachShader(_handle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    private static int CompileShader(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
            throw new Exception($"Shader compile error ({type}): {GL.GetShaderInfoLog(shader)}");
        return shader;
    }

    public void Use() => GL.UseProgram(_handle);

    public void SetInt(string name, int value)
    {
        if (!_uniformLocations.TryGetValue(name, out int loc))
        {
            loc = GL.GetUniformLocation(_handle, name);
            _uniformLocations[name] = loc;
        }
        GL.Uniform1(loc, value);
    }

    public void SetMatrix4(string name, Matrix4 mat)
    {
        if (!_uniformLocations.TryGetValue(name, out int loc))
        {
            loc = GL.GetUniformLocation(_handle, name);
            _uniformLocations[name] = loc;
        }
        GL.UniformMatrix4(loc, false, ref mat);
    }

    public void SetVector4(string name, Vector4 vec)
    {
        if (!_uniformLocations.TryGetValue(name, out int loc))
        {
            loc = GL.GetUniformLocation(_handle, name);
            _uniformLocations[name] = loc;
        }
        GL.Uniform4(loc, vec);
    }

    public void Dispose() => GL.DeleteProgram(_handle);
}