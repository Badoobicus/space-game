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

    [Export]
    private PackedScene _vesselsPrefab;

    private int _mapFocusIndex;

    private readonly Dictionary<string, CelestialBodyView> _celestialBodyViewsById = [];
    private readonly Dictionary<string, VesselView> _vesselViewsById = [];

    public override void _Ready()
    {
        _universe.UniverseInitialized += _OnUniverseInitialized;
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

    private void _OnUniverseInitialized()
    {
        var celestialBodies = _universe.GetCelestialBodies();

        foreach (var celestialBody in celestialBodies)
        {
            var celestialBodyView = _celestialBodyViewPrefab.Instantiate<CelestialBodyView>();
            celestialBodyView.Init(celestialBody);
            celestialBodyView.Name =
                $"{celestialBody.CelestialBodyId}_{celestialBodyView.GetType().Name}";
            AddChild(celestialBodyView);
            _celestialBodyViewsById.Add(celestialBody.CelestialBodyId, celestialBodyView);
        }

        var vessels = _universe.GetVessels();

        foreach (var vessel in vessels)
        {
            var vesselView = _vesselsPrefab.Instantiate<VesselView>();
            vesselView.Scale = Vector3.One * 0.03f;
            vesselView.Name = $"{vessel.VesselId}_{vesselView.GetType().Name}";
            AddChild(vesselView);
            _vesselViewsById.Add(vessel.VesselId, vesselView);
        }

        _mapFocusIndex = 0;
        _camera.OnMapFocusChange(
            _celestialBodyViewsById[celestialBodies[_mapFocusIndex].CelestialBodyId]
        );
    }

    private void _OnTimeChanged(long year, double time)
    {
        foreach (var (id, bodyView) in _celestialBodyViewsById)
        {
            var body = _universe.GetCelestialBody(id);
            bodyView.Position = (Vector3)OrbitUtils.CalculateAbsolutePosition(body);
        }

        foreach (var (id, vesselView) in _vesselViewsById)
        {
            var vessel = _universe.GetVessel(id);
            vesselView.Position = (Vector3)OrbitUtils.CalculateAbsolutePosition(vessel);
            vesselView.LookAt(vesselView.Position + (Vector3)vessel.Velocity, Vector3.Up);
        }
    }
}
