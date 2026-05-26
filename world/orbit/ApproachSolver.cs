using System;
using System.Diagnostics;
using Godot;

public class ApproachSolver
{
    public static double SolveClosestApproach(Orbit subject, Orbit target, double time)
    {
        var subjectState = EllipticalOrbitSolver.SolveState(subject, time);
        var targetState = EllipticalOrbitSolver.SolveState(target, time);

        var subjectSolver = OrbitSolver.FromInitialState(
            subject.Body.Mass,
            subjectState[0],
            subjectState[1],
            time
        );
        var targetSolver = OrbitSolver.FromInitialState(
            target.Body.Mass,
            targetState[0],
            targetState[1],
            time
        );

        double resultTime = -1;
        double resultDistanceSq = -1;

        double subjectPeriod = subjectSolver.Period;
        int steps = 32;
        for (int i = 0; i < steps; i++)
        {
            double t = time + i * (subjectPeriod / steps);
            var (subjectPosition, subjectVelocity) = subjectSolver.SolveStateAtTime(t);
            var (targetPosition, targetVelocity) = targetSolver.SolveStateAtTime(t);

            double subjectAltitude = subjectPosition.Length();
            double targetAltitude = targetPosition.Length();

            Vector3 relativePosition = targetPosition - subjectPosition;
            Vector3 relativeVelocity = targetVelocity - subjectVelocity;
            double d = relativePosition.Dot(relativeVelocity);

            double distanceSq = (subjectPosition - targetPosition).Length();

            // must be moving towards each other,
            // angle between positions must be less than pi/8,
            // subject altitude at closest approach must be around target altitude
            if (
                d < 0
                && subjectPosition.AngleTo(targetPosition) < Math.PI / 8
                && (resultDistanceSq < 0 || distanceSq < resultDistanceSq)
                && subjectAltitude > targetAltitude * 0.8
                && subjectAltitude < targetAltitude * 1.2
            )
            {
                resultTime = t;
                resultDistanceSq = distanceSq;
            }
        }

        return resultTime;
    }
}
