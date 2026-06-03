using System.Collections.Generic;
using Godot;

public partial class UniverseView : Node3D
{
    [Export]
    private Universe _universe;

    [Export]
    private CameraController _camera;

    [Export]
    private PackedScene _celestialBodyViewPrefab;

    private int _mapFocusIndex;

    private readonly Dictionary<string, CelestialBodyView> _celestialBodyViewsById = new();

    public override void _Ready()
    {
        _universe.CelestialBodiesInitialized += _OnCelestialBodiesInitialized;
        _universe.TimeChanged += _OnTimeChanged;
    }

    public override void _Input(InputEvent @event)
    {
        var celestialBodies = _universe.GetCelestialBodies();

        var mapFocusChanged = false;

        if (@event.IsActionPressed("map_focus_next"))
        {
            _mapFocusIndex = (_mapFocusIndex + 1) % celestialBodies.Count;
            mapFocusChanged = true;
        }
        else if (@event.IsActionPressed("map_focus_prev"))
        {
            _mapFocusIndex = (celestialBodies.Count + _mapFocusIndex - 1) % celestialBodies.Count;
            mapFocusChanged = true;
        }

        if (mapFocusChanged)
        {
            _camera.OnMapFocusChange(
                _celestialBodyViewsById[celestialBodies[_mapFocusIndex].CelestialBodyId]
            );
        }
    }

    private void _OnCelestialBodiesInitialized()
    {
        var celestialBodies = _universe.GetCelestialBodies();

        foreach (var celestialBody in celestialBodies)
        {
            var celestialBodyView = _celestialBodyViewPrefab.Instantiate<CelestialBodyView>();
            celestialBodyView.Init(celestialBody);
            AddChild(celestialBodyView);
            _celestialBodyViewsById.Add(celestialBody.CelestialBodyId, celestialBodyView);
        }

        _mapFocusIndex = 0;
        _camera.OnMapFocusChange(
            _celestialBodyViewsById[celestialBodies[_mapFocusIndex].CelestialBodyId]
        );
    }

    private void _OnTimeChanged(long year, double time)
    {
        var celestialBodies = _universe.GetCelestialBodies();

        foreach (var body in celestialBodies)
        {
            if (body.Orbit == null)
            {
                continue;
            }

            var state = body.OrbitSolver.SolveStateAtTime(time);
            body.Position = body.Orbit.Body.Position + state.Position;
            body.Velocity = state.Velocity;

            _celestialBodyViewsById[body.CelestialBodyId].Position = (Vector3)body.Position;
        }
    }
}
