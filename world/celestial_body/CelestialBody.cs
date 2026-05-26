using Godot;
using System;

public partial class CelestialBody : Node3D
{
    [Export]
    private Node3D _mesh;

    [Export]
    private Node3D _soiMesh;

    public double Mass { get; set; }
    public Orbit Orbit { get; set; }

    private double _radius;
    public double Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            _mesh.Scale = Vector3.One * (float)_radius * 2;
        }
    }

    private double _soiRadius;
    public double SoiRadius
    {
        get => _soiRadius;
        set
        {
            _soiRadius = value;
            _soiMesh.Scale = Vector3.One * (float)_soiRadius * 2;
        }
    }
}
