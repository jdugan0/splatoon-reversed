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
    private Vector2 planeSize;

    public override void _Ready()
    {
        planeSize = Mesh.Get("size").As<Vector2>();
        Mask.Size = new Vector2I(
            Mathf.CeilToInt(planeSize.X * MaskPixelsPerMeter),
            Mathf.CeilToInt(planeSize.Y * MaskPixelsPerMeter)
        );
        var mat = (ShaderMaterial)GetActiveMaterial(0).Duplicate();
        SetSurfaceOverrideMaterial(0, mat);
        mat.SetShaderParameter("dirty_tiling", planeSize);
        mat.SetShaderParameter("dirt_mask", Mask.GetTexture());
    }

    public void Paint(Vector3 worldHit)
    {
        var local = GlobalTransform.AffineInverse() * worldHit;
        var uv = new Vector2(local.X / planeSize.X + 0.5f, local.Z / planeSize.Y + 0.5f);
        Brush.Position = uv * (Vector2)Mask.Size;
        Mask.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
    }
}
