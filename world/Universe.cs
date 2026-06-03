using System;
using System.Collections.Generic;
using Godot;

public partial class Universe : Node
{
    [Signal]
    public delegate void CelestialBodiesInitializedEventHandler();

    [Signal]
    public delegate void TimeChangedEventHandler(long year, double time);

    [Signal]
    public delegate void TimeWarpChangeEventHandler(double timeWarp);

    private long _year;
    private double _time;
    private int _timeWarpStep;
    private double _timeWarp;
    private double _targetWarpTime;

    private readonly List<CelestialBody> _celestialBodies = new();
    private readonly Dictionary<string, CelestialBody> _celestialBodiesById = new();

    public override void _Ready()
    {
        var star = new CelestialBody
        {
            CelestialBodyId = "star",
            Mass = 100,
            Radius = 2,
            SoiRadius = 100,
        };
        _celestialBodies.Add(star);

        var planet1 = new CelestialBody
        {
            CelestialBodyId = "planet1",
            Orbit = Orbit.FromElements(star, 10, 0.05, 0, 0, 0, 0),
            Mass = 1,
            Radius = 0.25,
        };
        planet1.SoiRadius = OrbitUtils.CalculateSoiRadius(planet1);
        _celestialBodies.Add(planet1);

        var moon1 = new CelestialBody
        {
            CelestialBodyId = "moon1",
            Orbit = Orbit.FromElements(planet1, 1, 0, 0, 0, 0, 0),
            Mass = 0.05,
            Radius = 0.05,
        };
        moon1.SoiRadius = OrbitUtils.CalculateSoiRadius(moon1);
        _celestialBodies.Add(moon1);

        var planet2 = new CelestialBody
        {
            CelestialBodyId = "planet2",
            Orbit = Orbit.FromElements(
                star,
                30,
                0.2,
                Mathf.DegToRad(3),
                Mathf.DegToRad(90),
                Mathf.DegToRad(45),
                0
            ),
            Mass = 1,
            Radius = 0.5,
        };
        planet2.SoiRadius = OrbitUtils.CalculateSoiRadius(planet2);
        _celestialBodies.Add(planet2);

        var moon2 = new CelestialBody
        {
            CelestialBodyId = "moon2",
            Orbit = Orbit.FromElements(planet2, 1, 0, 0, 0, 0, 0),
            Mass = 0.05,
            Radius = 0.05,
        };
        moon2.SoiRadius = OrbitUtils.CalculateSoiRadius(moon2);
        _celestialBodies.Add(moon2);

        var moon3 = new CelestialBody
        {
            CelestialBodyId = "moon3",
            Orbit = Orbit.FromElements(planet2, 3, 0.1, Mathf.DegToRad(-5), 0, 0, 0),
            Mass = 0.05,
            Radius = 0.05,
        };
        moon3.SoiRadius = OrbitUtils.CalculateSoiRadius(moon3);
        _celestialBodies.Add(moon3);

        foreach (var celestialBody in _celestialBodies)
        {
            if (celestialBody.Orbit != null)
            {
                var initialState = EllipticalOrbitSolver.SolveState(celestialBody.Orbit, 0);
                celestialBody.OrbitSolver = OrbitSolver.FromInitialState(
                    initialState,
                    celestialBody.Orbit.Body.Mass,
                    0
                );
            }

            _celestialBodiesById.Add(celestialBody.CelestialBodyId, celestialBody);
        }

        EmitSignalCelestialBodiesInitialized();

        _year = 0;
        _time = 0;
        EmitSignalTimeChanged(_year, _time);

        _timeWarp = _CalculateTimeWarp(_timeWarpStep);
        EmitSignalTimeWarpChange(_timeWarp);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_targetWarpTime > 0)
        {
            _time = _targetWarpTime;
            _targetWarpTime = -1;
        }
        else
        {
            _time += delta * _timeWarp;
        }

        if (_time > Constants.SecondsPerYear)
        {
            _ShiftEpoch();
        }

        EmitSignalTimeChanged(_year, _time);
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

    public CelestialBody GetCelestialBody(string celestialBodyId)
    {
        return _celestialBodiesById.GetValueOrDefault(celestialBodyId, null);
    }

    public List<CelestialBody> GetCelestialBodies()
    {
        return _celestialBodies;
    }

    public double GetTime()
    {
        return _time;
    }

    public void SetTargetWarpTime(double targetWarpTime)
    {
        if (targetWarpTime > _time)
        {
            _targetWarpTime = targetWarpTime;
        }
    }

    private void _ShiftEpoch()
    {
        var shiftedYears = (long)_time / Constants.SecondsPerYear;
        var shiftedSeconds = shiftedYears * Constants.SecondsPerYear;

        foreach (var body in _celestialBodies)
        {
            if (body.Orbit == null)
            {
                continue;
            }

            body.OrbitSolver = body.OrbitSolver.WithEpoch(
                body.OrbitSolver.Period
                    + (body.OrbitSolver.Epoch - shiftedSeconds) % body.OrbitSolver.Period
            );
        }

        _year += shiftedYears;
        _time -= shiftedSeconds;
    }

    private double _CalculateTimeWarp(int timeWarpStep)
    {
        return Math.Pow(5, timeWarpStep);
    }
}
