using Godot;
using System;

public partial class CameraController : Camera3D
{
    private Vector2 rotation = new Vector2(0, 45);
    private float zoom = 30;
    private Vector2 lastMousePosition = Vector2.Zero;

    public override void _Process(double delta)
    {
        var mousePosition = this.GetViewport().GetMousePosition();

        if (Input.IsActionJustReleased("zoom_in"))
        {
            this.zoom -= 1;
        }

        if (Input.IsActionJustReleased("zoom_out"))
        {
            this.zoom += 1;
        }

        if (this.zoom < 1)
        {
            this.zoom = 1;
        }

        if (Input.IsActionPressed("rotate_camera"))
        {
            this.rotation += (mousePosition - this.lastMousePosition) * 0.4f * (float)delta;
        }

        this.Rotation = new Vector3(-this.rotation.Y, -this.rotation.X, 0);
        this.Position = this.Basis.Z * (float)Math.Pow(1.15f, this.zoom);

        this.lastMousePosition = mousePosition;
    }
}
