using System;
using System.Collections.Generic;
using Godot;

public partial class World : Node
{
    [Export]
    private CameraController _camera;

    [Export]
    private PackedScene _celestialBodyPrefab;

    [Export]
    private PackedScene _vesselPrefab;

    [Signal]
    public delegate void CelestialBodiesInitializedEventHandler();

    [Signal]
    public delegate void TimeChangedEventHandler(double time);

    [Signal]
    public delegate void TimeWarpChangeEventHandler(double timeWarp);

    private double _time;
    private int _timeWarpStep;
    private double _timeWarp;
    private int _mapFocusIndex;

    private readonly List<CelestialBody> _celestialBodies = new();
    private readonly Dictionary<string, CelestialBody> _celestialBodiesById = new();
    private readonly Dictionary<string, CelestialBodyView> _celestialBodyViewsById = new();

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

            var bodyView = _celestialBodyPrefab.Instantiate<CelestialBodyView>();
            bodyView.Init(celestialBody);
            AddChild(bodyView);
            _celestialBodyViewsById.Add(celestialBody.CelestialBodyId, bodyView);
        }

        EmitSignalCelestialBodiesInitialized();

        _time = 0;
        EmitSignalTimeChanged(_time);

        _timeWarp = _CalculateTimeWarp(_timeWarpStep);
        EmitSignalTimeWarpChange(_timeWarp);

        _mapFocusIndex = 0;
        _camera.OnMapFocusChange(
            _celestialBodyViewsById[_celestialBodies[_mapFocusIndex].CelestialBodyId]
        );
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

            var state = body.OrbitSolver.SolveStateAtTime(_time);
            body.Position = body.Orbit.Body.Position + state.Position;
            body.Velocity = state.Velocity;

            _celestialBodyViewsById[body.CelestialBodyId].Position = (Vector3)body.Position;
        }
    }

    public override void _Input(InputEvent @event)
    {
        var timeWarpChanged = false;
        var mapFocusChanged = false;

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
        else if (@event.IsActionPressed("map_focus_next"))
        {
            _mapFocusIndex = (_mapFocusIndex + 1) % _celestialBodies.Count;
            mapFocusChanged = true;
        }
        else if (@event.IsActionPressed("map_focus_prev"))
        {
            _mapFocusIndex = (_celestialBodies.Count + _mapFocusIndex - 1) % _celestialBodies.Count;
            mapFocusChanged = true;
        }

        if (timeWarpChanged)
        {
            _timeWarp = _CalculateTimeWarp(_timeWarpStep);
            EmitSignalTimeWarpChange(_timeWarp);
        }

        if (mapFocusChanged)
        {
            _camera.OnMapFocusChange(
                _celestialBodyViewsById[_celestialBodies[_mapFocusIndex].CelestialBodyId]
            );
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

    private double _CalculateTimeWarp(int timeWarpStep)
    {
        return Math.Pow(5, timeWarpStep);
    }
}
