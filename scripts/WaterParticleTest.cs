using Godot;
using System;

public partial class WaterParticleTest : GpuParticles3D
{
	[Export] GpuParticles3D particles;
    public override void _Ready()
    {
		particles.Emitting = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Input.IsActionJustPressed("fire"))
		{
			particles.Emitting = true;
		}
        if (Input.IsActionJustReleased("fire"))
		{
			particles.Emitting = false;
		}
	}

    // public override void _PhysicsProcess(double delta)
    // {
	// 	var direction = -GlobalTransform.Basis.Z;
	// 	GD.Print(direction);
    //     particles.ProcessMaterial.Set("direction", direction);
    // }

}