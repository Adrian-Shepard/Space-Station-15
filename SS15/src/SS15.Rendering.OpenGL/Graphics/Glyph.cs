using System.Numerics;

namespace SS15.Rendering.OpenGL.Graphics;

public readonly struct Glyph
{
    public readonly Vector2 Offset;
    public readonly Vector2 Size;
    public readonly Vector2 UV0;
    public readonly Vector2 UV1;
    public readonly float Advance;

    public Glyph(Vector2 offset, Vector2 size, Vector2 uv0, Vector2 uv1, float advance)
    {
        Offset = offset;
        Size = size;
        UV0 = uv0;
        UV1 = uv1;
        Advance = advance;
    }
}