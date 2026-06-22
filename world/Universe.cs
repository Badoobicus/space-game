using System;
using System.Collections.Generic;
using Godot;

public partial class Universe : Node
{
    [Signal]
    public delegate void UniverseInitializedEventHandler();

    [Signal]
    public delegate void TimeChangedEventHandler(long year, double time);

    [Signal]
    public delegate void TimeWarpChangeEventHandler(double timeWarp);

    private long _year;
    private double _time;
    private int _timeWarpStep;
    private double _timeWarp;
    private double _targetWarpTime;

    private readonly List<IOrbitable> _orbitables = new();
    private readonly Dictionary<string, CelestialBody> _celestialBodiesById = new();
    private readonly Dictionary<string, Vessel> _vesselsById = new();
    private readonly HashSet<Vessel> _vesselsWithDirtyPatches = new();

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

        _SimulateOrbitables();

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
        var shiftedYears = (long)_time / Constants.SecondsPerYear;
        var shiftedSeconds = shiftedYears * Constants.SecondsPerYear;

        foreach (var orbitable in _orbitables)
        {
            if (orbitable.Orbit == null)
            {
                continue;
            }

            orbitable.Orbit = orbitable.Orbit.WithEpoch(
                orbitable.Orbit.Period
                    + (orbitable.Orbit.Epoch - shiftedSeconds) % orbitable.Orbit.Period
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
