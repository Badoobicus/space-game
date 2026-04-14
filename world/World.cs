using Godot;
using System;
using System.Collections.Generic;

public partial class World : Node
{
    [Export]
    private PackedScene celestialBodyPrefab;

    [Export]
    private PackedScene vesselPrefab;

    [Signal]
    public delegate void WorldReadyEventHandler();

    private List<CelestialBody> celestialBodies = new List<CelestialBody>();
    public List<CelestialBody> CelestialBodies
    {
        get => new List<CelestialBody>(this.celestialBodies);
    }
    private List<VesselOld> vessels = new List<VesselOld>();
    public List<VesselOld> Vessels
    {
        get => new List<VesselOld>(this.vessels);
    }
    public double TimeWarp { get; set; } = 1;

    private double elapsedTime = 130;

    // private double elapsedTime = 0;
    public double ElapsedTime
    {
        get => this.elapsedTime;
    }
    private const double TIME_WARP_STEP_FACTOR = 10;
    private const double TIME_WARP_MIN = 0.01;
    private const double TIME_WARP_MAX = 1_000_000;

    public override void _Ready()
    {
        // CelestialBody star = this.celestialBodyPrefab.Instantiate<CelestialBody>();
        // this.AddChild(star);
        // star.Radius = 5;
        // star.Mass =  20;
        // celestialBodies.Add(star);

        CelestialBody star = this.celestialBodyPrefab.Instantiate<CelestialBody>();
        this.AddChild(star);
        star.Radius = 5;
        star.Mass = 2000;
        celestialBodies.Add(star);

        CelestialBody planet1 = this.celestialBodyPrefab.Instantiate<CelestialBody>();
        this.AddChild(planet1);
        planet1.Radius = 1;
        planet1.Mass = 1;
        planet1.Orbit = new Orbit(
            new OrbitalParameters
            {
                CentralBody = star,
                SemiMajorAxis = 15,
                Eccentricity = 0.9,
                Inclination = 0,
                ArgumentOfPeriapsis = 0,
                LongitudeOfAscendingNode = 0,
            }
        );
        celestialBodies.Add(planet1);

        CelestialBody moon1 = this.celestialBodyPrefab.Instantiate<CelestialBody>();
        this.AddChild(moon1);
        moon1.Radius = 0.33;
        moon1.Mass = 1;
        moon1.Orbit = new Orbit(
            new OrbitalParameters
            {
                CentralBody = planet1,
                SemiMajorAxis = 3,
                Eccentricity = 0,
                Inclination = 0,
                ArgumentOfPeriapsis = 0,
                LongitudeOfAscendingNode = 0,
            }
        );
        celestialBodies.Add(moon1);

        CelestialBody planet2 = this.celestialBodyPrefab.Instantiate<CelestialBody>();
        this.AddChild(planet2);
        planet2.Radius = 1.5;
        planet2.Mass = 1;
        planet2.Orbit = new Orbit(
            new OrbitalParameters
            {
                CentralBody = star,
                SemiMajorAxis = 40,
                Eccentricity = 0.2,
                Inclination = 0,
                ArgumentOfPeriapsis = 0,
                LongitudeOfAscendingNode = 0,
            }
        );
        celestialBodies.Add(planet2);

        VesselOld vessel1 = this.vesselPrefab.Instantiate<VesselOld>();
        this.AddChild(vessel1);
        vessel1.Orbit = new EllipticalOrbit
        {
            CelestialBody = star,
            SemiMajorAxis = 20,
            Eccentricity = 0.8,
            Inclination = 45 % (Math.PI * 2),
            ArgumentOfPeriapsis = 0,
            LongitudeOfAscendingNode = 0
        };
        vessels.Add(vessel1);

        VesselOld vessel2 = this.vesselPrefab.Instantiate<VesselOld>();
        this.AddChild(vessel2);
        vessel2.Orbit = new EllipticalOrbit
        {
            CelestialBody = star,
            SemiMajorAxis = 20,
            Eccentricity = 0,
            Inclination = 0,
            ArgumentOfPeriapsis = 0,
            LongitudeOfAscendingNode = 0
        };
        vessels.Add(vessel2);

        this.EmitSignal("WorldReady");
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("increase_time_warp"))
        {
            this.TimeWarp = Math.Min(this.TimeWarp * TIME_WARP_STEP_FACTOR, TIME_WARP_MAX);
        }

        if (Input.IsActionJustPressed("decrease_time_warp"))
        {
            this.TimeWarp = Math.Max(this.TimeWarp / TIME_WARP_STEP_FACTOR, TIME_WARP_MIN);
        }

        if (Input.IsActionJustPressed("clear_time_warp"))
        {
            this.TimeWarp = 1;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        foreach (CelestialBody celestialBody in this.celestialBodies)
        {
            celestialBody.Move(this);
        }

        foreach (VesselOld vessel in this.vessels)
        {
            vessel.Move(this);
        }

        this.elapsedTime += delta * this.TimeWarp;
    }
}
