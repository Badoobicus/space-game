using System;
using System.Collections.Generic;
using Godot;

public class TrajectorySolver
{
    private class PatchSolverStep
    {
        public Patch Patch;
        public Orbit NextOrbit;
    }

    private class InterceptState
    {
        public StateVector State;
        public double DistanceToTarget;
        public double DistanceVelocityToTarget;
    }

    public static Trajectory SolveTrajectory(Orbit orbit, UniverseTime startTime)
    {
        List<Patch> patches = [];
        PatchSolverStep prevPatchStep = null;

        while (
            _SolveNextPatchStep(
                prevPatchStep?.NextOrbit ?? orbit,
                prevPatchStep?.Patch?.EndTime ?? startTime
            )
                is { } patchStep
        )
        {
            patches.Add(patchStep.Patch);

            if (patchStep.Patch.EndTime == null)
            {
                break;
            }

            if (patches.Count > 10)
            {
                GD.PrintErr("Exceeded maximum patches threshold; breaking loop");
                break;
            }

            prevPatchStep = patchStep;
        }

        return new Trajectory(patches.ToArray());
    }

    private static PatchSolverStep _SolveNextPatchStep(Orbit orbit, UniverseTime startTime)
    {
        PatchSolverStep step = _SolveSoiEjectionPatchStep(orbit, startTime);

        step =
            _SolveInterceptPatchStep(
                orbit,
                startTime,
                step?.Patch?.EndTime ?? startTime.PlusSeconds(orbit.Period)
            ) ?? step;

        return step ?? new PatchSolverStep { Patch = new Patch(orbit, startTime, null) };
    }

    private static PatchSolverStep _SolveSoiEjectionPatchStep(Orbit orbit, UniverseTime startTime)
    {
        if (
            orbit.Apoapsis > 0 && orbit.Apoapsis <= orbit.CenterBody.SoiRadius
            || orbit.CenterBody.Orbit == null
        )
        {
            return null;
        }

        var timeAtSoi = orbit.SolveTimeAtRadius(orbit.CenterBody.SoiRadius);
        var timeAtPeriapsis = orbit.SolveTimeAtRadius(orbit.Periapsis);

        if (orbit.Apoapsis > 0)
        {
            var secondsFromPeriapsisToSoi = timeAtPeriapsis.SecondsUntil(timeAtSoi) % orbit.Period;

            if (secondsFromPeriapsisToSoi < 0)
            {
                secondsFromPeriapsisToSoi += orbit.Period;
            }

            if (secondsFromPeriapsisToSoi > orbit.Period / 2)
            {
                timeAtSoi = timeAtPeriapsis.PlusSeconds(orbit.Period - secondsFromPeriapsisToSoi);
            }

            timeAtSoi = timeAtSoi.PlusSeconds(
                orbit.Period * Math.Ceiling(-startTime.SecondsUntil(timeAtSoi) / orbit.Period)
            );
        }
        else
        {
            if (timeAtSoi.SecondsUntil(timeAtPeriapsis) > 0)
            {
                timeAtSoi = timeAtPeriapsis.PlusSeconds(timeAtSoi.SecondsUntil(timeAtPeriapsis));
            }
        }

        var state = orbit.SolveStateAtTime(timeAtSoi);
        var centerBodyState = orbit.CenterBody.Orbit.SolveStateAtTime(timeAtSoi);
        var newState = new StateVector(
            centerBodyState.Position + state.Position,
            centerBodyState.Velocity + state.Velocity
        );
        var newOrbit = Orbit.FromInitialState(
            orbit.CenterBody.Orbit.CenterBody,
            newState,
            timeAtSoi
        );

        return new PatchSolverStep
        {
            Patch = new Patch(orbit, startTime, timeAtSoi),
            NextOrbit = newOrbit,
        };
    }

    private static PatchSolverStep _SolveInterceptPatchStep(
        Orbit orbit,
        UniverseTime startTime,
        UniverseTime endTime
    )
    {
        UniverseTime? result = null;
        CelestialBody target = null;
        var celestialBodies = _DeterminePossibleInterceptTargets(orbit);

        foreach (var celestialBody in celestialBodies)
        {
            var time = _CalculateInterceptTime(orbit, celestialBody, startTime, endTime);

            if (result == null || time < result)
            {
                result = time;
                target = celestialBody;
            }
        }

        if (result is { } resultTime)
        {
            var state = orbit.SolveStateAtTime(resultTime);
            var targetState = target.Orbit.SolveStateAtTime(resultTime);
            var newState = new StateVector(
                state.Position - targetState.Position,
                state.Velocity - targetState.Velocity
            );
            var newOrbit = Orbit.FromInitialState(target, newState, resultTime);

            return new PatchSolverStep
            {
                Patch = new Patch(orbit, startTime, resultTime),
                NextOrbit = newOrbit,
            };
        }

        return null;
    }

    private static List<CelestialBody> _DeterminePossibleInterceptTargets(Orbit orbit)
    {
        List<CelestialBody> result = new();

        foreach (var celestialBody in orbit.CenterBody.OrbitingCelestialBodies)
        {
            if (
                orbit.Apoapsis >= celestialBody.Orbit.Periapsis - celestialBody.SoiRadius
                && orbit.Periapsis <= celestialBody.Orbit.Apoapsis + celestialBody.SoiRadius
            )
            {
                result.Add(celestialBody);
            }
        }

        return result;
    }

    private static UniverseTime? _CalculateInterceptTime(
        Orbit orbit,
        CelestialBody target,
        UniverseTime startTime,
        UniverseTime endTime
    )
    {
        if (orbit.Apoapsis < 0)
        {
            throw new NotImplementedException(
                "Intercept solver not yet implemented for non-elliptical orbits"
            );
        }

        UniverseTime? lastTime = null;
        InterceptState lastInterceptState = null;

        var resolution = 32;

        for (int i = 0; i <= resolution; i++)
        {
            UniverseTime time = startTime.PlusSeconds(
                startTime.SecondsUntil(endTime) * ((double)i / resolution)
            );
            InterceptState interceptState = _CalculateInterceptState(orbit, target.Orbit, time);

            if (
                lastTime.HasValue
                && lastInterceptState.DistanceToTarget > target.SoiRadius
                && lastInterceptState.DistanceVelocityToTarget < 0
                && (
                    interceptState.DistanceToTarget < target.SoiRadius
                    || interceptState.DistanceVelocityToTarget > 0
                )
            )
            {
                var interceptTime = _NarrowInterceptTime(orbit, lastTime.Value, time, target);

                if (interceptTime.HasValue)
                {
                    return interceptTime;
                }
            }

            lastTime = time;
            lastInterceptState = interceptState;
        }

        return null;
    }

    private static UniverseTime? _NarrowInterceptTime(
        Orbit orbit,
        UniverseTime startTime,
        UniverseTime endTime,
        CelestialBody target
    )
    {
        UniverseTime lowerTime = startTime;
        InterceptState lowerInterceptState = _CalculateInterceptState(
            orbit,
            target.Orbit,
            lowerTime
        );

        UniverseTime upperTime = endTime;
        InterceptState upperInterceptState = _CalculateInterceptState(
            orbit,
            target.Orbit,
            upperTime
        );

        int i = 0;

        while (
            lowerInterceptState.State.Position.DistanceTo(upperInterceptState.State.Position)
            > lowerInterceptState.DistanceToTarget - target.SoiRadius
        )
        {
            if (i >= 100)
            {
                GD.PrintErr("Exceeded threshold for intercept calculator; breaking loop");
                return null;
            }

            var time = lowerTime.PlusSeconds(lowerTime.SecondsUntil(upperTime) / 2);

            InterceptState interceptState = _CalculateInterceptState(orbit, target.Orbit, time);

            if (
                interceptState.DistanceToTarget <= target.SoiRadius
                || interceptState.DistanceVelocityToTarget > 0
            )
            {
                upperTime = time;
                upperInterceptState = interceptState;
            }
            else
            {
                lowerTime = time;
                lowerInterceptState = interceptState;
            }

            i++;
        }

        return null;
    }

    private static InterceptState _CalculateInterceptState(
        Orbit orbit,
        Orbit targetOrbit,
        UniverseTime time
    )
    {
        StateVector state = orbit.SolveStateAtTime(time);
        StateVector targetState = targetOrbit.SolveStateAtTime(time);

        Vector3d relPos = targetState.Position - state.Position;
        double distance = relPos.Length();
        Vector3d relVel = targetState.Velocity - state.Velocity;
        double distanceVel = relVel.Dot(relPos) / distance;

        return new InterceptState
        {
            State = state,
            DistanceToTarget = distance,
            DistanceVelocityToTarget = distanceVel,
        };
    }
}
