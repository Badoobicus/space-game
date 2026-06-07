using System;

public class ApproachSolver
{
    public static double SolveClosestApproach(
        CelestialBody subject,
        CelestialBody target,
        double time
    )
    {
        double resultTime = -1;
        double resultDistanceSq = -1;

        double subjectPeriod = subject.Orbit.Period;
        int steps = 32;
        for (int i = 0; i < steps; i++)
        {
            double t = time + i * (subjectPeriod / steps);

            var subjectState = subject.Orbit.SolveStateAtTime(t);
            var subjectPosition = subjectState.Position;
            var subjectVelocity = subjectState.Velocity;

            var targetState = target.Orbit.SolveStateAtTime(t);
            var targetPosition = targetState.Position;
            var targetVelocity = targetState.Velocity;

            double subjectAltitude = subjectPosition.Length();
            double targetAltitude = targetPosition.Length();

            Vector3d relativePosition = targetPosition - subjectPosition;
            Vector3d relativeVelocity = targetVelocity - subjectVelocity;
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
