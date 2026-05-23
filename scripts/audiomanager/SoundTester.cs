using Godot;
using System;

public partial class SoundTester : Node
{
    public override void _UnhandledInput(InputEvent @event)
    {
        if (Input.IsActionJustPressed("fire"))
		{
			AudioManager.instance.PlaySFX();
		}
	}
}