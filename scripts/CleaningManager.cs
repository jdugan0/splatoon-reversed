using System;
using System.Collections.Generic;
using Godot;

public partial class CleaningManager : Node
{
    public static CleaningManager I { get; private set; }

    public override void _Ready() => I = this;

    private readonly Dictionary<DirtyWall, (float area, float dirt)> walls = new();

    public void Register(DirtyWall w, float area) => walls[w] = (area, 1f);

    public void Report(DirtyWall w, float dirt)
    {
        if (walls.TryGetValue(w, out var v))
            walls[w] = (v.area, dirt);
    }

    public float CleanFraction()
    {
        float a = 0,
            d = 0;
        foreach (var (area, dirt) in walls.Values)
        {
            a += area;
            d += area * dirt;
        }
        return a > 0 ? 1f - d / a : 1f;
    }
}
