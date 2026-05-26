using Godot;
using System;

public partial class CameraController : Camera3D
{
    private Vector2 _rotation = new Vector2(0, 45);
    private float _zoom = 20;
    private Vector2 _lastMousePosition = Vector2.Zero;

    public override void _Process(double delta)
    {
        var mousePosition = GetViewport().GetMousePosition();

        if (Input.IsActionJustReleased("zoom_in"))
        {
            _zoom -= 1;
        }

        if (Input.IsActionJustReleased("zoom_out"))
        {
            _zoom += 1;
        }

        if (_zoom < 1)
        {
            _zoom = 1;
        }

        if (Input.IsActionPressed("rotate_camera"))
        {
            _rotation += (mousePosition - _lastMousePosition) * 0.4f * (float)delta;
        }

        Rotation = new Vector3(-_rotation.Y, -_rotation.X, 0);
        Position = Basis.Z * (float)Math.Pow(1.15f, _zoom);

        _lastMousePosition = mousePosition;
    }
}
