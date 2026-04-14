using Godot;
using System;

public class EllipticalOrbitSegment : OrbitSegment
{
    public double Period
    {
        get =>
            (Math.PI * 2)
            * Math.Sqrt(
                (
                    this.OrbitalParameters.SemiMajorAxis
                    * this.OrbitalParameters.SemiMajorAxis
                    * this.OrbitalParameters.SemiMajorAxis
                ) / this.OrbitalParameters.CentralBody.Mass
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

    public EllipticalOrbitSegment(OrbitalParameters parameters)
        : base(parameters) { }

    public override OrbitalState GetStateAtTime(double time)
    {
        double meanAnomaly = this.GetMeanAnomalyAtTime(time);
        double eccentricAnomaly = this.GetEccentricAnomalyAtMeanAnomaly(meanAnomaly);
        double trueAnomaly = this.GetTrueAnomalyAtEccentricAnomaly(eccentricAnomaly);
        return this.GetStateAtAnomalies(eccentricAnomaly, trueAnomaly);
    }

    public OrbitalState GetStateAtMeanAnomaly(double meanAnomaly)
    {
        double eccentricAnomaly = this.GetEccentricAnomalyAtMeanAnomaly(meanAnomaly);
        double trueAnomaly = this.GetTrueAnomalyAtEccentricAnomaly(eccentricAnomaly);
        return this.GetStateAtAnomalies(eccentricAnomaly, trueAnomaly);
    }

    public OrbitalState GetStateAtTrueAnomaly(double trueAnomaly)
    {
        double eccentricAnomaly = this.GetEccentricAnomalyAtTrueAnomaly(trueAnomaly);
        return this.GetStateAtAnomalies(eccentricAnomaly, trueAnomaly);
    }

    public double GetEccentricAnomalyAtTrueAnomaly(double trueAnomaly)
    {
        double eccentricAnomaly =
            Math.Atan2(
                Math.Sqrt(1 - this.OrbitalParameters.Eccentricity) * Math.Sin(trueAnomaly / 2),
                Math.Sqrt(1 + this.OrbitalParameters.Eccentricity) * Math.Cos(trueAnomaly / 2)
            ) * 2;
        return this.ConformRadianMultiple(eccentricAnomaly, trueAnomaly);
    }

    public OrbitalState GetStateAtEccentricAnomaly(double eccentricAnomaly)
    {
        double trueAnomaly = this.GetTrueAnomalyAtEccentricAnomaly(eccentricAnomaly);
        return this.GetStateAtAnomalies(eccentricAnomaly, trueAnomaly);
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
        double eccentricAnomaly =
            this.OrbitalParameters.Eccentricity > 0.8 ? Math.PI : clampedMeanAnomaly;
        int iterations = 0;

        do
        {
            prevEccentricAnomaly = eccentricAnomaly;
            eccentricAnomaly =
                eccentricAnomaly
                - (
                    (
                        eccentricAnomaly
                        - this.OrbitalParameters.Eccentricity * Math.Sin(eccentricAnomaly)
                        - clampedMeanAnomaly
                    ) / (1 - this.OrbitalParameters.Eccentricity * Math.Cos(eccentricAnomaly))
                );
            prevDifference = difference;
            difference = Math.Abs(eccentricAnomaly - prevEccentricAnomaly);
            iterations++;
        } while (Math.Abs(difference - prevDifference) > 1e-12 && iterations < 100);

        return this.ConformRadianMultiple(eccentricAnomaly, meanAnomaly);
    }

    public double GetTrueAnomalyAtEccentricAnomaly(double eccentricAnomaly)
    {
        double trueAnomaly =
            Math.Atan2(
                Math.Sqrt(1 + this.OrbitalParameters.Eccentricity) * Math.Sin(eccentricAnomaly / 2),
                Math.Sqrt(1 - this.OrbitalParameters.Eccentricity) * Math.Cos(eccentricAnomaly / 2)
            ) * 2;
        return this.ConformRadianMultiple(trueAnomaly, eccentricAnomaly);
    }

    private OrbitalState GetStateAtAnomalies(double eccentricAnomaly, double trueAnomaly)
    {
        double distance = this.GetDistanceAtEccentricAnomaly(eccentricAnomaly);

        Vector3D position = this.OrbitalParameters.ToInertialFrame(
            distance * Math.Cos(trueAnomaly),
            distance * Math.Sin(trueAnomaly)
        );

        double velocityFactor =
            Math.Sqrt(
                this.OrbitalParameters.CentralBody.Mass * this.OrbitalParameters.SemiMajorAxis
            ) / distance;
        Vector3D velocity = this.OrbitalParameters.ToInertialFrame(
            velocityFactor * -Math.Sin(eccentricAnomaly),
            velocityFactor
                * Math.Sqrt(
                    1 - this.OrbitalParameters.Eccentricity * this.OrbitalParameters.Eccentricity
                )
                * Math.Cos(eccentricAnomaly)
        );

        return new OrbitalState
        {
            Position = position,
            Velocity = velocity,
            CentralBody = this.OrbitalParameters.CentralBody
        };
    }

    private double GetDistanceAtEccentricAnomaly(double eccentricAnomaly)
    {
        return this.OrbitalParameters.SemiMajorAxis
            * (1 - this.OrbitalParameters.Eccentricity * Math.Cos(eccentricAnomaly));
    }

    private double ConformRadianMultiple(double sourceRadians, double targetRadians)
    {
        return ((sourceRadians + (Math.PI * 2)) % (Math.PI * 2))
            + (targetRadians - (targetRadians % (Math.PI * 2)));
    }
}
