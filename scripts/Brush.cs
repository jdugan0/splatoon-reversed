using Godot;

public partial class Brush : Node3D
{
    public override void _Ready() { }

    public override void _Process(double delta)
    {
        // GD.Print(GlobalTransform.Basis.Z);
        if (Input.IsActionPressed("fire"))
        {
            var spaceState = GetWorld3D().DirectSpaceState;
            var origin = GlobalTransform.Origin;
            var direction = -GlobalTransform.Basis.Z;

            var query = PhysicsRayQueryParameters3D.Create(origin, origin + direction * 1000f);
            var result = spaceState.IntersectRay(query);

            if (result.Count > 0)
            {
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
}
