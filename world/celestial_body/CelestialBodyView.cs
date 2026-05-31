using Godot;

public partial class CelestialBodyView : Node3D
{
    [Export]
    private Node3D _mesh;

    [Export]
    private Node3D _soiMesh;

    private double _radius;
    private double _soiRadius;

    public void Init(CelestialBody celestialBody)
    {
        _radius = celestialBody.Radius;
        _mesh.Scale = Vector3.One * (float)_radius * 2;

        _soiRadius = celestialBody.SoiRadius;
        _soiMesh.Scale = Vector3.One * (float)_soiRadius * 2;
    }
}
