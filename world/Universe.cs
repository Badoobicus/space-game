using System;
using System.Collections.Generic;
using Godot;

public partial class Universe : Node
{
    [Signal]
    public delegate void UniverseInitializedEventHandler();

    [Signal]
    public delegate void TimeChangedEventHandler(long year, double seconds);

    [Signal]
    public delegate void TimeWarpChangeEventHandler(double timeWarp);

    private UniverseTime _time;
    private int _timeWarpStep;
    private double _timeWarp;
    private UniverseTime _targetWarpTime;

    private readonly List<IOrbitable> _orbitables = [];
    private readonly Dictionary<string, CelestialBody> _celestialBodiesById = [];
    private readonly Dictionary<string, Vessel> _vesselsById = [];
    private readonly HashSet<Vessel> _vesselsWithDirtyPatches = [];

    public override void _Ready()
    {
        List<CelestialBody> celestialBodies = CelestialBodyConfig.CreateCelestialBodies();

        foreach (var celestialBody in celestialBodies)
        {
            _celestialBodiesById.Add(celestialBody.CelestialBodyId, celestialBody);
            _orbitables.Add(celestialBody);
        }

        List<Vessel> vessels = VesselConfig.CreateVessels(_celestialBodiesById);

        foreach (var vessel in vessels)
        {
            _vesselsById.Add(vessel.VesselId, vessel);
            _orbitables.Add(vessel);
        }

        _SimulateOrbitables();

        EmitSignalUniverseInitialized();

        _time = UniverseTime.Zero;
        EmitSignalTimeChanged(_time.MajorUnits, _time.Seconds);

        _timeWarp = _CalculateTimeWarp(_timeWarpStep);
        EmitSignalTimeWarpChange(_timeWarp);
    }

    public override void _PhysicsProcess(double delta)
    {
        var prevTime = _time;

        if (_targetWarpTime > _time)
        {
            _time = _targetWarpTime;
            _targetWarpTime = UniverseTime.Zero;
        }
        else
        {
            _time = _time.PlusSeconds(_timeWarp * delta);
        }

        if (_time.MajorUnits > prevTime.MajorUnits)
        {
            _ShiftEpoch();
        }

        _SimulateOrbitables();

        EmitSignalTimeChanged(_time.MajorUnits, _time.Seconds);
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
        return new(_celestialBodiesById.Values);
    }

    public Vessel GetVessel(string vesselId)
    {
        return _vesselsById.GetValueOrDefault(vesselId, null);
    }

    public List<Vessel> GetVessels()
    {
        return new(_vesselsById.Values);
    }

    public UniverseTime GetTime()
    {
        return _time;
    }

    public void SetTargetWarpTime(UniverseTime targetWarpTime)
    {
        if (
            targetWarpTime.MajorUnits > _time.MajorUnits
            || (
                targetWarpTime.MajorUnits == _time.MajorUnits
                && targetWarpTime.Seconds > _time.Seconds
            )
        )
        {
            _targetWarpTime = targetWarpTime;
        }
    }

    public List<Vessel> PopVesselsWithDirtyPatches()
    {
        var result = new List<Vessel>(_vesselsWithDirtyPatches);
        _vesselsWithDirtyPatches.Clear();
        return result;
    }

    private void _SimulateOrbitables()
    {
        foreach (var orbitable in _orbitables)
        {
            if (orbitable.Orbit == null)
            {
                continue;
            }

            if (orbitable is Vessel vessel)
            {
                var prevTrajectory = vessel.Trajectory;

                try
                {
                    vessel.Trajectory =
                        vessel.Trajectory == null
                            ? TrajectorySolver.SolveTrajectory(vessel.Orbit, _time)
                            : vessel.Trajectory.FilterActivePatches(_time);
                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to solve trajectory for vessel {vessel.VesselId}",
                        ex
                    );
                }

                vessel.Orbit = vessel.Trajectory.CurrentPatch.Orbit;

                if (
                    prevTrajectory == null
                    || vessel.Trajectory.CurrentPatch != prevTrajectory.CurrentPatch
                )
                {
                    _vesselsWithDirtyPatches.Add(vessel);
                }
            }

            var state = orbitable.Orbit.SolveStateAtTime(_time);
            orbitable.Position = state.Position;
            orbitable.Velocity = state.Velocity;
        }
    }

    private void _ShiftEpoch()
    {
        foreach (var orbitable in _orbitables)
        {
            if (orbitable.Orbit == null)
            {
                continue;
            }

            if (orbitable is CelestialBody celestialBody)
            {
                celestialBody.Orbit = celestialBody.Orbit.WithTargetEpoch(_time);
            }
            else if (orbitable is Vessel vessel)
            {
                vessel.Trajectory = vessel.Trajectory.WithTargetEpoch(_time);
                vessel.Orbit = vessel.Trajectory.CurrentPatch.Orbit;
            }
        }
    }

    private double _CalculateTimeWarp(int timeWarpStep)
    {
        return Math.Pow(5, timeWarpStep);
    }
}
