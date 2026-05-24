using Godot;

public partial class Brush : Node3D
{
    private MeshInstance3D currBeam = null;

    [Export]
    private Node3D muzzle;

    public override void _Ready() { }

    public override void _Process(double delta)
    {
        // GD.Print(GlobalTransform.Basis.Z);
        if (currBeam != null)
        {
            currBeam.QueueFree();
        }
        currBeam = null;
        if (Input.IsActionPressed("fire"))
        {
            var spaceState = GetWorld3D().DirectSpaceState;
            var origin = GlobalTransform.Origin;
            var direction = -GlobalTransform.Basis.Z;

            var query = PhysicsRayQueryParameters3D.Create(origin, origin + direction * 1000f);
            var result = spaceState.IntersectRay(query);

            if (result.Count > 0)
            {
                var hitPos = result["position"].As<Vector3>();
                if (muzzle.GlobalPosition.DistanceTo(hitPos) > 0.75f)
                {
                    currBeam = MakeCylinder(muzzle.GlobalPosition, hitPos, 0.1f);
                    var worldXform = currBeam.Transform;
                    AddChild(currBeam);
                    currBeam.GlobalTransform = worldXform;
                }
                var collider = result["collider"].As<Node>().GetParent();
                if (collider != null && collider is DirtyWall c)
                {
                    c.Paint(result["position"].AsVector3(), DirtyWall.SplatType.BRUSH);
                }
            }
        }

        //SFX
        if (Input.IsActionJustPressed("fire"))
        {
            AudioManager.instance.PlaySFX("gun-spray-start");
            AudioManager.instance.PlaySFX("gun-spray-loop");
            AudioManager.instance.CancelSFX("gun-spray-end");
        }
        if (Input.IsActionJustReleased("fire"))
        {
            AudioManager.instance.PlaySFX("gun-spray-end");
            AudioManager.instance.CancelSFX("gun-spray-start");
            AudioManager.instance.CancelSFX("gun-spray-loop");
        }
        //END OF SFX
    }

    static MeshInstance3D MakeCylinder(Vector3 a, Vector3 b, float radius)
    {
        var dir = b - a;
        var len = dir.Length();
        var up = dir / len;
        var refVec = Mathf.Abs(up.Dot(Vector3.Up)) > 0.999f ? Vector3.Right : Vector3.Up;
        var right = up.Cross(refVec).Normalized();
        return new MeshInstance3D
        {
            Mesh = new CylinderMesh
            {
                Height = len,
                TopRadius = radius,
                BottomRadius = radius,
            },
            Transform = new Transform3D(new Basis(right, up, right.Cross(up)), (a + b) * 0.5f),
        };
    }
}
