using System;
using Godot;

public partial class DirtyWall : MeshInstance3D
{
    [Export]
    public float MaskPixelsPerMeter = 256f;

    [Export]
    private SubViewport Mask;

    [Export]
    private Sprite2D Brush;

    [Export]
    private Sprite2D WalkSplat;

    [Export]
    private Sprite2D JumpSplat;
    private Vector2 planeSize;

    private double measureTimer;

    [Export]
    private double MeasureInterval = 0.2;

    private Image cachedMask;
    [Export]
    private CollisionShape3D collisionShape;
    [Export]
    private MeshInstance3D parentBox;

    public class SplatType
    {
        public static readonly SplatType BRUSH = new();
        public static readonly SplatType WALK = new();
        public static readonly SplatType JUMP = new();

        public Sprite2D Brush { get; set; }
    }

    public override void _Ready()
    {
        SplatType.BRUSH.Brush = Brush;
        SplatType.WALK.Brush = WalkSplat;
        SplatType.JUMP.Brush = JumpSplat;
        Vector3 a = parentBox.GetAabb().Size;
        GD.Print(a);
        Mesh.Set("size", new Vector3(a.X,a.Z,0.01f));
        planeSize = Mesh.Get("size").As<Vector2>();
        collisionShape.Shape.Set("size", new Vector3(planeSize.X, 0, planeSize.Y));
        Mask.Size = new Vector2I(
            Mathf.CeilToInt(planeSize.X * MaskPixelsPerMeter),
            Mathf.CeilToInt(planeSize.Y * MaskPixelsPerMeter)
        );
        var mat = (ShaderMaterial)GetActiveMaterial(0).Duplicate();
        SetSurfaceOverrideMaterial(0, mat);
        mat.SetShaderParameter("dirty_tiling", planeSize);
        mat.SetShaderParameter("dirt_mask", Mask.GetTexture());
        CleaningManager.I.Register(this, planeSize.X * planeSize.Y);
    }

    public void Paint(Vector3 worldHit, SplatType splatType)
    {
        Sprite2D brush = splatType.Brush;
        var local = GlobalTransform.AffineInverse() * worldHit;
        var uv = new Vector2(local.X / planeSize.X + 0.5f, local.Z / planeSize.Y + 0.5f);
        brush.Position = uv * (Vector2)Mask.Size;
        Mask.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
    }

    public override void _Process(double delta)
    {
        measureTimer += delta;
        if (measureTimer < MeasureInterval)
            return;
        measureTimer = 0;
        //Measure();
    }

    private void Measure()
    {
        var img = Mask.GetTexture().GetImage();
        img.Resize(16, 16, Image.Interpolation.Bilinear);
        cachedMask = img;
        float sum = 0;
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 16; x++)
            sum += img.GetPixel(x, y).R;
        CleaningManager.I.Report(this, 1f - sum / 256f);
    }

    public float? SampleDirtAt(Vector3 worldHit)
    {
        if (cachedMask == null)
            return null;
        var local = GlobalTransform.AffineInverse() * worldHit;
        float u = local.X / planeSize.X + 0.5f;
        float v = local.Z / planeSize.Y + 0.5f;
        if (u < 0 || u > 1 || v < 0 || v > 1)
            return null;
        int w = cachedMask.GetWidth(),
            h = cachedMask.GetHeight();
        int x = Mathf.Clamp((int)(u * w), 0, w - 1);
        int y = Mathf.Clamp((int)(v * h), 0, h - 1);
        return 1f - cachedMask.GetPixel(x, y).R;
    }
}
