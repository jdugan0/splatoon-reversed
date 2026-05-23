using Godot;
using System;

public partial class SoundTester : Node
{
	[Export] AudioStreamPlayer player;
    public override void _UnhandledInput(InputEvent @event)
    {
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
	}
}