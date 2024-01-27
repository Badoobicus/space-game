using Godot;
using System;
using System.Collections.Generic;

public partial class World : Node
{
    [Export]
    private Node3D sphere1;

    [Export]
    private Node3D sphere2;

    [Export]
    private Node3D sphere3;

    private List<CelestialBody> celestialBodies = new List<CelestialBody>();

    public override void _Ready()
    {
        var star = new CelestialBody();
        star.Radius = 5;
        star.Mass = 1;

        var planet1 = new CelestialBody();
        planet1.Radius = 1;
        planet1.Mass = 1;
        planet1.Orbit = new Orbit();
        planet1.Orbit.CelestialBody = star;
        planet1.Orbit.SemiMajorAxis = 5;
        planet1.Orbit.Eccentricity = 0;
        planet1.Orbit.Inclination = 0;
        planet1.Orbit.ArgumentOfPeriapsis = 0;
        planet1.Orbit.LongitudeOfAscendingNode = 0;

        var moon1 = new CelestialBody();
        moon1.Radius = 0.33;
        moon1.Mass = 1;
        moon1.Orbit = new Orbit();
        moon1.Orbit.CelestialBody = planet1;
        moon1.Orbit.SemiMajorAxis = 1;
        moon1.Orbit.Eccentricity = 0;
        moon1.Orbit.Inclination = 0;
        moon1.Orbit.ArgumentOfPeriapsis = 0;
        moon1.Orbit.LongitudeOfAscendingNode = 0;

        celestialBodies.Add(star);
        celestialBodies.Add(planet1);
        celestialBodies.Add(moon1);
    }

    public override void _PhysicsProcess(double delta)
    {
        double time = Time.GetUnixTimeFromSystem() * 10;

        this.sphere1.Scale = Vector3.One * (float)this.celestialBodies[0].Radius;
        this.sphere1.Position = Vector3.Zero;

        this.sphere2.Scale = Vector3.One * (float)this.celestialBodies[1].Radius;
        this.sphere2.Position =
            this.sphere1.Position + this.celestialBodies[1].Orbit.GetPositionAtTime(time);

        this.sphere3.Scale = Vector3.One * (float)this.celestialBodies[2].Radius;
        this.sphere3.Position =
            this.sphere2.Position + this.celestialBodies[2].Orbit.GetPositionAtTime(time);
    }
}
