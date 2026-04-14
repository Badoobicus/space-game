using Godot;
using System;

public partial class WorldUi : Node
{
    [Export]
    private World world;

    [Export]
    private Label elapsedTimeLabel;

    public override void _Process(double delta)
    {
        elapsedTimeLabel.Text = world.ElapsedTime.ToString("##.##");
    }
}
