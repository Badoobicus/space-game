using Godot;
using System;
using System.Collections.Generic;

public partial class World : Node
{
    [Export]
    private PackedScene _celestialBodyPrefab;

    [Export]
    private PackedScene _vesselPrefab;

    [Signal]
    public delegate void TimeChangedEventHandler(double time);

    [Signal]
    public delegate void TimeWarpChangeEventHandler(double timeWarp);

    private double _time;
    private int _timeWarpStep;
    private double _timeWarp;
    private readonly List<CelestialBody> _celestialBodies = new();

    public override void _Ready()
    {
        _time = 0;
        EmitSignalTimeChanged(_time);

        _timeWarp = _CalculateTimeWarp(_timeWarpStep);
        EmitSignalTimeWarpChange(_timeWarp);

        var star = _celestialBodyPrefab.Instantiate<CelestialBody>();
        star.Mass = 100;
        star.Radius = 2;
        star.SoiRadius = 100;
        AddChild(star);
        _celestialBodies.Add(star);

        var planet1 = _celestialBodyPrefab.Instantiate<CelestialBody>();
        planet1.Orbit = Orbit.FromElements(star, 10, 0.05, 0, 0, 0, 0);
        planet1.Mass = 1;
        planet1.Radius = 0.25;
        planet1.SoiRadius = OrbitUtils.CalculateSoiRadius(planet1);
        AddChild(planet1);
        _celestialBodies.Add(planet1);

        var moon1 = _celestialBodyPrefab.Instantiate<CelestialBody>();
        moon1.Orbit = Orbit.FromElements(planet1, 1, 0, 0, 0, 0, 0);
        moon1.Mass = 0.05;
        moon1.Radius = 0.05;
        moon1.SoiRadius = OrbitUtils.CalculateSoiRadius(moon1);
        AddChild(moon1);
        _celestialBodies.Add(moon1);

        var planet2 = _celestialBodyPrefab.Instantiate<CelestialBody>();
        planet2.Orbit = Orbit.FromElements(
            star,
            30,
            0.2,
            Mathf.DegToRad(3),
            Mathf.DegToRad(90),
            Mathf.DegToRad(45),
            0
        );
        planet2.Mass = 1;
        planet2.Radius = 0.5;
        planet2.SoiRadius = OrbitUtils.CalculateSoiRadius(planet2);
        AddChild(planet2);
        _celestialBodies.Add(planet2);

        var moon2 = _celestialBodyPrefab.Instantiate<CelestialBody>();
        moon2.Orbit = Orbit.FromElements(planet2, 1, 0, 0, 0, 0, 0);
        moon2.Mass = 0.05;
        moon2.Radius = 0.05;
        moon2.SoiRadius = OrbitUtils.CalculateSoiRadius(moon2);
        AddChild(moon2);
        _celestialBodies.Add(moon2);

        var moon3 = _celestialBodyPrefab.Instantiate<CelestialBody>();
        moon3.Orbit = Orbit.FromElements(planet2, 3, 0.1, Mathf.DegToRad(-5), 0, 0, 0);
        moon3.Mass = 0.05;
        moon3.Radius = 0.05;
        moon3.SoiRadius = OrbitUtils.CalculateSoiRadius(moon3);
        AddChild(moon3);
        _celestialBodies.Add(moon3);
    }

    public override void _PhysicsProcess(double delta)
    {
        _time += delta * _timeWarp;
        EmitSignalTimeChanged(_time);

        foreach (var body in _celestialBodies)
        {
            if (body.Orbit == null)
            {
                continue;
            }

            var orbit = body.Orbit;
            var state = EllipticalOrbitSolver.SolveState(orbit, 0);
            var solver = OrbitSolver.FromInitialState(orbit.Body.Mass, state[0], state[1], 0);
            var (position, _) = solver.SolveStateAtTime(_time);
            body.Position = body.Orbit.Body.Position + position;
        }
    }

    public override void _Input(InputEvent @event)
    {
        var timeWarpChanged = false;

        if (@event.IsActionPressed("time_warp_increase"))
        {
            _timeWarpStep++;
            timeWarpChanged = true;
        }
        else if (@event.IsActionPressed("time_warp_decrease"))
        {
            _timeWarpStep--;
            timeWarpChanged = true;
        }
        else if (@event.IsActionPressed("time_warp_reset"))
        {
            _timeWarpStep = 0;
            timeWarpChanged = true;
        }

        if (timeWarpChanged)
        {
            _timeWarp = _CalculateTimeWarp(_timeWarpStep);
            EmitSignalTimeWarpChange(_timeWarp);
        }
    }

    public List<CelestialBody> GetCelestialBodies()
    {
        return _celestialBodies;
    }

    public double GetTime()
    {
        return _time;
    }

    private double _CalculateTimeWarp(int timeWarpStep)
    {
        return Math.Pow(5, timeWarpStep);
    }
}
