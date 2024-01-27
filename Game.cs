using Godot;
using System;

public partial class Game : Node
{
    [Export]
    private PackedScene startingScene;

    public override void _Ready()
    {
        this.AddChild(this.startingScene.Instantiate());
    }
}
