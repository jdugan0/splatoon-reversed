using Godot;

public partial class Brush : Node3D
{
    private MeshInstance3D currBeam = null;

    [Export]
    private Movement player;

    [Export]
    private float recoilForce = 10f;

    [Export]
    GpuParticles3D particleHit;

    [Export]
    private Node3D muzzle;

    [Export]
    private float GunRange;

    [Export]
    private Material beamMat;

    [Export]
    GpuParticles3D muzzleParticle;

    [Export(PropertyHint.Range, "0,1,0.01")]
    private float scatterChance = 0.35f;

    [Export]
    private float scatterMinDist = 0.15f;

    [Export]
    private float scatterMaxDist = 0.5f;

    [Export]
    private float scatterMinScale = 0.3f;

    [Export]
    private float scatterMaxScale = 0.6f;

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
                var hitNormal = result["normal"].As<Vector3>();
                var incident = (hitPos - muzzle.GlobalPosition).Normalized();
                var reflected = -(incident - 2f * incident.Dot(hitNormal) * hitNormal).Normalized();
                // player.AddForce(-(recoilForce * (hitPos - player.GlobalPosition).Normalized()));
                currBeam = MakeCylinder(currBeam, muzzle.GlobalPosition, hitPos, 0.1f);
                var worldXform = currBeam.Transform;
                currBeam.GlobalTransform = worldXform;
                var collider = result["collider"].As<Node>().GetParent();
                if (collider != null && collider is DirtyWall c)
                {
                    Vector3 paintPos = hitPos;
                    float paintScale = 1f;
                    if (GD.Randf() < scatterChance)
                    {
                        var nRef =
                            Mathf.Abs(hitNormal.Dot(Vector3.Up)) > 0.999f
                                ? Vector3.Right
                                : Vector3.Up;
                        var tangent = hitNormal.Cross(nRef).Normalized();
                        var bitangent = hitNormal.Cross(tangent).Normalized();
                        float angle = GD.Randf() * Mathf.Tau;
                        float dist = (float)GD.RandRange(scatterMinDist, scatterMaxDist);
                        paintPos =
                            hitPos
                            + (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)) * dist;
                        paintScale = (float)GD.RandRange(scatterMinScale, scatterMaxScale);
                    }
                    c.Paint(paintPos, DirtyWall.SplatType.BRUSH, paintScale);
                }

                var refVec =
                    Mathf.Abs(reflected.Dot(Vector3.Up)) > 0.999f ? Vector3.Right : Vector3.Up;
                var xAxis = reflected.Cross(refVec).Normalized();
                var yAxis = reflected.Cross(xAxis).Normalized();
                particleHit.GlobalTransform = new Transform3D(
                    new Basis(xAxis, yAxis, reflected),
                    hitPos
                );
                particleHit.Emitting = true;
            }
            else
            {
                particleHit.Emitting = false;
            }
        }
        else
        {
            if (currBeam != null)
            {
                currBeam.QueueFree();
                currBeam = null;
            }
            particleHit.Emitting = false;
        }

        //SFX
        if (Input.IsActionJustPressed("fire"))
        {
            AudioManager.instance.PlaySFX("gun-spray-start");
            AudioManager.instance.PlaySFX("gun-spray-loop");
            AudioManager.instance.CancelSFX("gun-spray-end");
            muzzleParticle.Emitting = true;
        }
        if (Input.IsActionJustReleased("fire"))
        {
            AudioManager.instance.PlaySFX("gun-spray-end");
            AudioManager.instance.CancelSFX("gun-spray-start");
            AudioManager.instance.CancelSFX("gun-spray-loop");
            muzzleParticle.Emitting = false;
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
                    Material = beamMat,
                    CapTop = false,
                    CapBottom = false,
                    Rings = Mathf.CeilToInt(10 * len),
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
            Material = beamMat,
            CapTop = false,
            CapBottom = false,
            Rings = Mathf.CeilToInt(10 * len),
        };
        beam.Transform = new Transform3D(new Basis(right, up, right.Cross(up)), (a + b) * 0.5f);
        return beam;
    }
}
