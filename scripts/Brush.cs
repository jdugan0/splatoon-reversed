using Godot;

public partial class Brush : Node3D
{
    private MeshInstance3D currBeam = null;

    [Export]
    private Movement player;

    [Export]
    private float recoilForce = 10f;

    [Export]
    private Node3D muzzle;

    [Export]
    private float GunRange;

    public override void _Ready() { }

    public override void _Process(double delta)
    {
        // GD.Print(GlobalTransform.Basis.Z);
        if (Input.IsActionPressed("fire"))
        {
            var spaceState = GetWorld3D().DirectSpaceState;
            var origin = GlobalTransform.Origin;
            var direction = -GlobalTransform.Basis.Z;

            var query = PhysicsRayQueryParameters3D.Create(origin, origin + direction * GunRange);
            var result = spaceState.IntersectRay(query);

            if (result.Count > 0)
            {
                var hitPos = result["position"].As<Vector3>();
                // player.AddForce(-(recoilForce * (hitPos - player.GlobalPosition).Normalized()));
                if (muzzle.GlobalPosition.DistanceTo(hitPos) > 0.75f)
                {
                    currBeam = MakeCylinder(currBeam, muzzle.GlobalPosition, hitPos, 0.1f);
                    var worldXform = currBeam.Transform;
                    currBeam.GlobalTransform = worldXform;
                }
                var collider = result["collider"].As<Node>().GetParent();
                if (collider != null && collider is DirtyWall c)
                {
                    c.Paint(result["position"].AsVector3(), DirtyWall.SplatType.BRUSH);
                }
            }
        }
        else
        {
            if (currBeam != null)
            {
                currBeam.QueueFree();
                currBeam = null;
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

    private MeshInstance3D MakeCylinder(MeshInstance3D beam, Vector3 a, Vector3 b, float radius)
    {
        var dir = b - a;
        var len = dir.Length();
        var up = dir / len;
        var refVec = Mathf.Abs(up.Dot(Vector3.Up)) > 0.999f ? Vector3.Right : Vector3.Up;
        var right = up.Cross(refVec).Normalized();
        if (beam == null)
        {
            var b1 = new MeshInstance3D
            {
                Mesh = new CylinderMesh
                {
                    Height = len,
                    TopRadius = radius,
                    BottomRadius = radius,
                },
                Transform = new Transform3D(new Basis(right, up, right.Cross(up)), (a + b) * 0.5f),
            };
            muzzle.AddChild(b1);
            return b1;
        }
        beam.Mesh = new CylinderMesh
        {
            Height = len,
            TopRadius = radius,
            BottomRadius = radius,
        };
        beam.Transform = new Transform3D(new Basis(right, up, right.Cross(up)), (a + b) * 0.5f);
        return beam;
    }
}
