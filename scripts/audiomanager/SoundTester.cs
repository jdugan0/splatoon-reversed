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

		
        if (Input.IsActionJustPressed("move_forward"))
		{
			AudioManager.instance.PlaySFX("movement-screech-start");
			AudioManager.instance.PlaySFX("movement-screech-loop");
			AudioManager.instance.CancelSFX("movement-screech-end");
		}
        if (Input.IsActionJustReleased("move_forward"))
		{
			AudioManager.instance.PlaySFX("movement-screech-end");
			AudioManager.instance.CancelSFX("movement-screech-loop");
			AudioManager.instance.CancelSFX("movement-screech-start");
		}
	}
}