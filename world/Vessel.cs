using Godot;
using System;

public partial class Vessel : Node3D
{
    public Orbit Orbit { get; set; }

    public void Move(World world)
    {
        if (this.Orbit != null)
        {
            OrbitalState state = this.Orbit.GetStateAtTime(world.ElapsedTime);
            this.Position = state.CentralBody.GlobalPosition + (Vector3)state.Position;
        }
        else
        {
            this.Position = Vector3.Zero;
        }
    }
}
