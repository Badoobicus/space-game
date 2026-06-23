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

    public static Trajectory SolveTrajectory(Orbit orbit, double startTime)
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
        double startTime
    )
    {
        if (prevPatchStep != null && double.IsPositiveInfinity(prevPatchStep.Patch.EndTime))
        {
            return null;
        }

        if (orbit.Apoapsis > orbit.CenterBody.SoiRadius || orbit.Apoapsis < 0)
        {
            var timeAtPeriapsis = orbit.SolveTimeAtRadius(orbit.Periapsis) % orbit.Period;

            if (timeAtPeriapsis < 0)
            {
                timeAtPeriapsis += orbit.Period;
            }

            var timeAtSoi = orbit.SolveTimeAtRadius(orbit.CenterBody.SoiRadius) % orbit.Period;

            if (timeAtSoi < 0)
            {
                timeAtSoi += orbit.Period;
            }

            if (timeAtSoi > timeAtPeriapsis + orbit.Period / 2)
            {
                timeAtSoi = orbit.Period - timeAtSoi;
            }

            if (timeAtSoi < startTime)
            {
                timeAtSoi += orbit.Period * Math.Ceiling((startTime - timeAtSoi) / orbit.Period);
            }

            if (orbit.CenterBody.Orbit == null)
            {
                throw new InvalidOperationException(
                    $"Unable to exit SOI of celestial body {orbit.CenterBody.CelestialBodyId}; "
                        + "celestial body does not have a center body"
                );
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

        return new PatchSolverStep { Patch = new Patch(orbit, startTime, double.PositiveInfinity) };
    }
}
