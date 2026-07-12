using System;
using System.Collections.Generic;
using Godot;

public class TrajectorySolver
{
    private class PatchSolverStep
    {
        public Patch Patch { get; set; }
        public Orbit NextOrbit { get; set; }
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
        return null;
    }
}
