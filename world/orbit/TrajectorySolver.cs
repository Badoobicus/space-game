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
                prevPatchStep,
                prevPatchStep?.NextOrbit ?? orbit,
                prevPatchStep?.Patch?.EndTime ?? startTime
            )
                is { } patchStep
        )
        {
            patches.Add(patchStep.Patch);
            prevPatchStep = patchStep;

            if (patches.Count > 10)
            {
                GD.PrintErr("Exceeded maximum patches threshold; breaking loop");
                break;
            }
        }

        return new Trajectory(patches.ToArray());
    }

    private static PatchSolverStep _SolveNextPatchStep(
        PatchSolverStep prevPatchStep,
        Orbit orbit,
        UniverseTime startTime
    )
    {
        if (prevPatchStep != null && prevPatchStep.Patch.EndTime == null)
        {
            return null;
        }

        if (orbit.Apoapsis > orbit.CenterBody.SoiRadius || orbit.Apoapsis < 0)
        {
            if (orbit.CenterBody.Orbit == null)
            {
                throw new InvalidOperationException(
                    $"Unable to exit SOI of celestial body {orbit.CenterBody.CelestialBodyId}; "
                        + "celestial body does not have a center body"
                );
            }

            var timeAtSoi = orbit.SolveTimeAtRadius(orbit.CenterBody.SoiRadius);
            var timeAtPeriapsis = orbit.SolveTimeAtRadius(orbit.Periapsis);
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

        return new PatchSolverStep { Patch = new Patch(orbit, startTime, null) };
    }
}
