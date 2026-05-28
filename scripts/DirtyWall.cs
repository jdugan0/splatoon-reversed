using System;
using Godot;

public partial class DirtyWall : MeshInstance3D
{
    [Export]
    public float MaskPixelsPerMeter = 256f;

    [Export]
    private SubViewport Mask;

    [Export]
    private Sprite2D Splat;

    [Export]
    private Texture2D BrushTexture;

    [Export]
    private Texture2D WalkTexture;

    [Export]
    private Texture2D JumpTexture;
    private Vector2 planeSize;

    private float[,] shadowMask;
    private float dirtSum;

    [Export]
    private CollisionShape3D collisionShape;

    [Export]
    private MeshInstance3D parentBox;

    [Export]
    private float scaleFactor = 0.5f;

    public enum SplatType
    {
        BRUSH,
        WALK,
        JUMP,
    }

    public override void _Ready()
    {
        Mesh = (Mesh)Mesh.Duplicate();
        if (parentBox != null)
        {
            Vector3 a = parentBox.GetAabb().Size;
            Mesh.Set("size", new Vector3(a.X, a.Z, 0.01f));
        }
        planeSize = Mesh.Get("size").As<Vector2>();
        collisionShape.Shape = (Shape3D)collisionShape.Shape.Duplicate();
        collisionShape.Shape.Set("size", new Vector3(planeSize.X, 0, planeSize.Y));
        Mask.Size = new Vector2I(
            Mathf.CeilToInt(planeSize.X * MaskPixelsPerMeter * scaleFactor),
            Mathf.CeilToInt(planeSize.Y * MaskPixelsPerMeter * scaleFactor)
        );
        var mat = (ShaderMaterial)GetActiveMaterial(0).Duplicate();
        SetSurfaceOverrideMaterial(0, mat);
        mat.SetShaderParameter("dirty_tiling", planeSize);
        mat.SetShaderParameter("dirt_mask", Mask.GetTexture());
        Mask.RenderTargetClearMode = SubViewport.ClearMode.Once;
        Mask.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;

        int sw = Mathf.Max(1, Mask.Size.X / 8);
        int sh = Mathf.Max(1, Mask.Size.Y / 8);
        shadowMask = new float[sw, sh];
        for (int y = 0; y < sh; y++)
        for (int x = 0; x < sw; x++)
            shadowMask[x, y] = 1f;
        dirtSum = sw * sh;

        CleaningManager.I.Register(this, planeSize.X * planeSize.Y);
    }

    public void Paint(Vector3 worldHit, SplatType splatType, float scale = 1f)
    {
        Splat.Texture = splatType switch
        {
            SplatType.WALK => WalkTexture,
            SplatType.JUMP => JumpTexture,
            _ => BrushTexture,
        };
        Splat.Scale = Vector2.One * scale * scaleFactor;
        Splat.Visible = true;
        var local = GlobalTransform.AffineInverse() * worldHit;
        var uv = new Vector2(local.X / planeSize.X + 0.5f, local.Z / planeSize.Y + 0.5f);
        Splat.Position = uv * (Vector2)Mask.Size;
        Mask.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;

        ApplyAnalyticalSplat(uv, Splat.Texture, scale);
    }

    private void ApplyAnalyticalSplat(Vector2 uv, Texture2D tex, float scale)
    {
        int w = shadowMask.GetLength(0);
        int h = shadowMask.GetLength(1);
        float rx = (tex.GetWidth() / (float)Mask.Size.X) * w * 0.5f * scale;
        float ry = (tex.GetHeight() / (float)Mask.Size.Y) * h * 0.5f * scale;
        if (rx <= 0f || ry <= 0f)
            return;
        float cx = uv.X * w;
        float cy = uv.Y * h;
        int x0 = Mathf.Max(0, (int)(cx - rx));
        int x1 = Mathf.Min(w - 1, (int)(cx + rx));
        int y0 = Mathf.Max(0, (int)(cy - ry));
        int y1 = Mathf.Min(h - 1, (int)(cy + ry));
        float invRx2 = 1f / (rx * rx);
        float invRy2 = 1f / (ry * ry);
        float delta = 0f;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            float dx = x + 0.5f - cx;
            float dy = y + 0.5f - cy;
            float d2 = dx * dx * invRx2 + dy * dy * invRy2;
            if (d2 >= 1f)
                continue;
            float before = shadowMask[x, y];
            float after = Mathf.Max(0f, before - (1f - d2));
            shadowMask[x, y] = after;
            delta += before - after;
        }
        if (delta > 0f)
        {
            dirtSum -= delta;
            CleaningManager.I.Report(this, dirtSum / (w * h));
        }
    }

    public float? SampleDirtAt(Vector3 worldHit)
    {
        if (shadowMask == null)
            return null;
        var local = GlobalTransform.AffineInverse() * worldHit;
        float u = local.X / planeSize.X + 0.5f;
        float v = local.Z / planeSize.Y + 0.5f;
        if (u < 0 || u > 1 || v < 0 || v > 1)
            return null;
        int w = shadowMask.GetLength(0);
        int h = shadowMask.GetLength(1);
        int x = Mathf.Clamp((int)(u * w), 0, w - 1);
        int y = Mathf.Clamp((int)(v * h), 0, h - 1);
        return shadowMask[x, y];
    }
}
