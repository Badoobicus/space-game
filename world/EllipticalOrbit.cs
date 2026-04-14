using System;

public class EllipticalOrbit
{
    // Keplerian orbit elements => Cartesian state vectors
    // See https://downloads.rene-schwarz.com/download/M001-Keplerian_Orbit_Elements_to_Cartesian_State_Vectors.pdf

    // Cartesian state vectors => Keplerian orbit elements
    // See https://downloads.rene-schwarz.com/download/M002-Cartesian_State_Vectors_to_Keplerian_Orbit_Elements.pdf

    // More orbit stuff
    // http://www.braeunig.us/space/orbmech.htm

    public CelestialBody CelestialBody { get; set; }
    public double SemiMajorAxis { get; set; }
    public double Eccentricity { get; set; }
    public double Inclination { get; set; }
    public double ArgumentOfPeriapsis { get; set; }
    public double LongitudeOfAscendingNode { get; set; }
    public double Period
    {
        get =>
            (Math.PI * 2)
            * Math.Sqrt(
                (this.SemiMajorAxis * this.SemiMajorAxis * this.SemiMajorAxis)
                    / this.CelestialBody.Mass
            );
    }
    public double Periapsis
    {
        get => this.GetDistanceAtEccentricAnomaly(Math.PI);
    }
    public double Apoapsis
    {
        get => this.GetDistanceAtEccentricAnomaly(0);
    }

    public EllipticalOrbit() { }

    // TODO temp constructor, remove later
    public EllipticalOrbit(OrbitalParameters parameters)
    {
        this.CelestialBody = parameters.CentralBody;
        this.SemiMajorAxis = parameters.SemiMajorAxis;
        this.Eccentricity = parameters.Eccentricity;
        this.ArgumentOfPeriapsis = parameters.ArgumentOfPeriapsis;
        this.LongitudeOfAscendingNode = parameters.LongitudeOfAscendingNode;
        this.Inclination = parameters.Inclination;
    }

    public double GetTimeAtMeanAnomaly(double meanAnomaly)
    {
        return this.Period * (meanAnomaly / (Math.PI * 2));
    }

    public double GetTimeAtEccentricAnomaly(double eccentricAnomaly)
    {
        double meanAnomaly = this.GetMeanAnomalyAtEccentricAnomaly(eccentricAnomaly);
        return this.GetTimeAtMeanAnomaly(meanAnomaly);
    }

    public double GetTimeAtTrueAnomaly(double trueAnomaly)
    {
        double eccentricAnomaly = this.GetEccentricAnomalyAtTrueAnomaly(trueAnomaly);
        return this.GetTimeAtEccentricAnomaly(eccentricAnomaly);
    }

    public double GetMeanAnomalyAtTime(double time)
    {
        return (time / this.Period) * (Math.PI * 2);
    }

    public double GetMeanAnomalyAtEccentricAnomaly(double eccentricAnomaly)
    {
        double meanAnomaly = eccentricAnomaly - this.Eccentricity * Math.Sin(eccentricAnomaly);
        return this.ConformRadianMultiple(meanAnomaly, eccentricAnomaly);
    }

    public double GetEccentricAnomalyAtTime(double time)
    {
        double meanAnomaly = this.GetMeanAnomalyAtTime(time);
        return this.GetEccentricAnomalyAtMeanAnomaly(meanAnomaly);
    }

    public double GetEccentricAnomalyAtMeanAnomaly(double meanAnomaly)
    {
        // Math works out better in range [0, 2 * pi)
        double clampedMeanAnomaly = meanAnomaly % (Math.PI * 2);

        // Repeatedly improve estimation of eccentric anomaly until estimates stop improving, which
        // happens when the difference between estimates stops decreasing, at which point we have reached
        // the best estimate a double-precision floating point calculation can handle.

        // Normally, this happens after just a single iteration for circular orbits, and around 10
        // iterations for highly elliptical orbits.

        double prevDifference;
        double difference = 2 * Math.PI;
        double prevEccentricAnomaly;
        double eccentricAnomaly = this.Eccentricity > 0.8 ? Math.PI : clampedMeanAnomaly;
        int iterations = 0;

        do
        {
            prevEccentricAnomaly = eccentricAnomaly;
            eccentricAnomaly =
                eccentricAnomaly
                - (
                    (
                        eccentricAnomaly
                        - this.Eccentricity * Math.Sin(eccentricAnomaly)
                        - clampedMeanAnomaly
                    ) / (1 - this.Eccentricity * Math.Cos(eccentricAnomaly))
                );
            prevDifference = difference;
            difference = Math.Abs(eccentricAnomaly - prevEccentricAnomaly);
            iterations++;
        } while (Math.Abs(difference - prevDifference) > 1e-12 && iterations < 100);

        return this.ConformRadianMultiple(eccentricAnomaly, meanAnomaly);
    }

    public double GetEccentricAnomalyAtTrueAnomaly(double trueAnomaly)
    {
        double eccentricAnomaly =
            Math.Atan2(
                Math.Sqrt(1 - this.Eccentricity) * Math.Sin(trueAnomaly / 2),
                Math.Sqrt(1 + this.Eccentricity) * Math.Cos(trueAnomaly / 2)
            ) * 2;
        return this.ConformRadianMultiple(eccentricAnomaly, trueAnomaly);
    }

    private double GetEccentricAnomalyAtTrueAnomalyAlternate(double trueAnomaly)
    {
        double eccentricAnomaly = Math.Acos(
            (this.Eccentricity + Math.Cos(trueAnomaly))
                / (1 + this.Eccentricity * Math.Cos(trueAnomaly))
        );
        return this.ConformRadianMultiple(eccentricAnomaly, trueAnomaly);
    }

    public double GetTrueAnomalyAtTime(double time)
    {
        double meanAnomaly = this.GetMeanAnomalyAtTime(time);
        return this.GetTrueAnomalyAtMeanAnomaly(meanAnomaly);
    }

    public double GetTrueAnomalyAtMeanAnomaly(double meanAnomaly)
    {
        double eccentricAnomaly = this.GetEccentricAnomalyAtMeanAnomaly(meanAnomaly);
        return this.GetTrueAnomalyAtEccentricAnomaly(eccentricAnomaly);
    }

    public double GetTrueAnomalyAtEccentricAnomaly(double eccentricAnomaly)
    {
        double trueAnomaly =
            Math.Atan2(
                Math.Sqrt(1 + this.Eccentricity) * Math.Sin(eccentricAnomaly / 2),
                Math.Sqrt(1 - this.Eccentricity) * Math.Cos(eccentricAnomaly / 2)
            ) * 2;
        return this.ConformRadianMultiple(trueAnomaly, eccentricAnomaly);
    }

    public double GetDistanceAtEccentricAnomaly(double eccentricAnomaly)
    {
        return this.SemiMajorAxis * (1 - this.Eccentricity * Math.Cos(eccentricAnomaly));
    }

    public Vector3D GetPositionAtTime(double time)
    {
        double meanAnomaly = this.GetMeanAnomalyAtTime(time);
        return this.GetPositionAtMeanAnomaly(meanAnomaly);
    }

    public Vector3D GetPositionAtMeanAnomaly(double meanAnomaly)
    {
        double eccentricAnomaly = this.GetEccentricAnomalyAtMeanAnomaly(meanAnomaly);
        return this.GetPositionAtEccentricAnomaly(eccentricAnomaly);
    }

    public Vector3D GetPositionAtEccentricAnomaly(double eccentricAnomaly)
    {
        double trueAnomaly = this.GetTrueAnomalyAtEccentricAnomaly(eccentricAnomaly);
        return this.GetPositionAtAnomalies(eccentricAnomaly, trueAnomaly);
    }

    public Vector3D GetPositionAtTrueAnomaly(double trueAnomaly)
    {
        double eccentricAnomaly = this.GetEccentricAnomalyAtTrueAnomaly(trueAnomaly);
        return this.GetPositionAtAnomalies(eccentricAnomaly, trueAnomaly);
    }

    private Vector3D GetPositionAtAnomalies(double eccentricAnomaly, double trueAnomaly)
    {
        double distance = this.GetDistanceAtEccentricAnomaly(eccentricAnomaly);
        double orbitalFramePositionX = distance * Math.Cos(trueAnomaly);
        double orbitalFramePositionY = distance * Math.Sin(trueAnomaly);
        return this.TransformOrbitalFrameToInertialFrame(
            orbitalFramePositionX,
            orbitalFramePositionY
        );
    }

    public Vector3D GetVelocityAtTime(double time)
    {
        double meanAnomaly = this.GetMeanAnomalyAtTime(time);
        return this.GetVelocityAtMeanAnomaly(meanAnomaly);
    }

    public Vector3D GetVelocityAtMeanAnomaly(double meanAnomaly)
    {
        double eccentricAnomaly = this.GetEccentricAnomalyAtMeanAnomaly(meanAnomaly);
        return this.GetVelocityAtEccentricAnomaly(eccentricAnomaly);
    }

    public Vector3D GetVelocityAtEccentricAnomaly(double eccentricAnomaly)
    {
        double trueAnomaly = this.GetTrueAnomalyAtEccentricAnomaly(eccentricAnomaly);
        return this.GetVelocityAtAnomalies(eccentricAnomaly, trueAnomaly);
    }

    public Vector3D GetVelocityAtTrueAnomalyAndDistance(double trueAnomaly)
    {
        double eccentricAnomaly = this.GetEccentricAnomalyAtTrueAnomaly(trueAnomaly);
        double distance = this.GetDistanceAtEccentricAnomaly(eccentricAnomaly);
        return this.GetVelocityAtAnomalies(eccentricAnomaly, trueAnomaly);
    }

    private Vector3D GetVelocityAtAnomalies(double eccentricAnomaly, double trueAnomaly)
    {
        double distance = this.GetDistanceAtEccentricAnomaly(eccentricAnomaly);
        double value = Math.Sqrt(this.CelestialBody.Mass * this.SemiMajorAxis) / distance;
        double orbitalFrameVelocityX = value * -Math.Sin(eccentricAnomaly);
        double orbitalFrameVelocityY =
            value
            * Math.Sqrt(1 - this.Eccentricity * this.Eccentricity)
            * Math.Cos(eccentricAnomaly);
        return this.TransformOrbitalFrameToInertialFrame(
            orbitalFrameVelocityX,
            orbitalFrameVelocityY
        );
    }

    public Vector3D TransformOrbitalFrameToInertialFrame(double x, double y)
    {
        double sinAoP = Math.Sin(this.ArgumentOfPeriapsis);
        double cosAoP = Math.Cos(this.ArgumentOfPeriapsis);
        double sinI = Math.Sin(this.Inclination);
        double cosI = Math.Cos(this.Inclination);
        double sinLoAN = Math.Sin(this.LongitudeOfAscendingNode);
        double cosLoAN = Math.Cos(this.LongitudeOfAscendingNode);
        return new Vector3D(
            x * (cosAoP * cosLoAN - sinAoP * cosI * sinLoAN)
                - y * (sinAoP * cosLoAN + cosAoP * cosI * sinLoAN),
            x * (sinAoP * sinI) + y * (cosAoP * sinI),
            -(
                x * (cosAoP * sinLoAN + sinAoP * cosI * cosLoAN)
                + y * (-sinAoP * sinLoAN + cosAoP * cosI * cosLoAN)
            )
        );
    }

    private double ConformRadianMultiple(double sourceRadians, double targetRadians)
    {
        return ((sourceRadians + (Math.PI * 2)) % (Math.PI * 2))
            + (targetRadians - (targetRadians % (Math.PI * 2)));
    }

    public static EllipticalOrbit FromPositionAndVelocity(
        Vector3D position,
        Vector3D velocity,
        CelestialBody celestialBody
    )
    {
        Vector3D orbitalMomentumVector = position.Cross(velocity);
        Vector3D eccentricityVector =
            (velocity.Cross(orbitalMomentumVector) / celestialBody.Mass)
            - (position / position.Length());
        Vector3D ascendingNodeVector = new Vector3D(0, 1, 0).Cross(orbitalMomentumVector);

        Vector3D normalizedPosition = position.Normalized();
        Vector3D normalizedEccentricityVector = eccentricityVector.Normalized();
        Vector3D normalizedAscendingNodeVector = ascendingNodeVector.Normalized();

        EllipticalOrbit orbit = new EllipticalOrbit
        {
            CelestialBody = celestialBody,
            SemiMajorAxis =
                1 / ((2 / position.Length()) - (velocity.LengthSquared() / celestialBody.Mass)),
            Eccentricity = eccentricityVector.Length(),
            Inclination = Math.Acos(orbitalMomentumVector.Y / orbitalMomentumVector.Length()),
            ArgumentOfPeriapsis =
                velocity.Y == 0
                    ? 0
                    : (
                        eccentricityVector.Y >= 0
                            ? Math.Acos(
                                normalizedAscendingNodeVector.Dot(normalizedEccentricityVector)
                            )
                            : (Math.PI * 2)
                                - Math.Acos(
                                    normalizedAscendingNodeVector.Dot(normalizedEccentricityVector)
                                )
                    ),
            LongitudeOfAscendingNode =
                velocity.Y == 0
                    ? 0
                    : (
                        ascendingNodeVector.Y >= 0
                            ? Math.Acos(ascendingNodeVector.X / ascendingNodeVector.Length())
                            : (Math.PI * 2)
                                - Math.Acos(ascendingNodeVector.X / ascendingNodeVector.Length())
                    ),
        };

        double trueAnomaly =
            position.Dot(velocity) >= 0
                ? Math.Acos(normalizedEccentricityVector.Dot(normalizedPosition))
                : (Math.PI * 2) - Math.Acos(normalizedEccentricityVector.Dot(normalizedPosition));
        double eccentricAnomaly = orbit.GetEccentricAnomalyAtTrueAnomaly(trueAnomaly);
        double meanAnomaly = orbit.GetMeanAnomalyAtEccentricAnomaly(eccentricAnomaly);

        return orbit;
    }
}
