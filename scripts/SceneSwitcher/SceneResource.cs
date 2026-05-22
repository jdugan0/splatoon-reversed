using System;
using Godot;

[GlobalClass]
public partial class SceneResource : Resource
{
    [Export]
    public string sceneName;

    [Export]
    public PackedScene scene;
}
