using Godot;
using System;

public class OrbitalState
{
    public Vector3D Position { get; set; }
    public Vector3D Velocity { get; set; }
    public CelestialBody CentralBody { get; set; }

    private readonly OrbitalParameters orbitalParameters;

    private bool calculatedTime = false;
    private double time = 0;

    public double Time
    {
        get
        {
            if (!this.calculatedTime)
            {
                throw new NotImplementedException("Cannot calculate time from mean anomaly");
            }

            return this.time;
        }
    }

    private bool calculatedMeanAnomaly = false;
    private double meanAnomaly = 0;
    public double MeanAnomaly
    {
        get
        {
            if (!this.calculatedMeanAnomaly)
            {
                if (this.calculatedTime)
                {
                    this.meanAnomaly = this.CalculateMeanAnomalyAtTime(this.Time);
                }
                else if (
                    this.calculatedMeanAnomaly
                    || this.calculatedEccentricAnomaly
                    || this.calculatedTrueAnomaly
                )
                {
                    throw new NotImplementedException(
                        "Cannot calculate mean anomaly from eccentric anomaly"
                    );
                }
                else
                {
                    throw new NotSupportedException("Missing data to calculate mean anomaly");
                }

                this.calculatedMeanAnomaly = true;
            }

            return this.meanAnomaly;
        }
    }

    private bool calculatedEccentricAnomaly = false;
    private double eccentricAnomaly = 0;
    public double EccentricAnomaly
    {
        get
        {
            if (!this.calculatedEccentricAnomaly)
            {
                if (this.calculatedTime || this.calculatedMeanAnomaly)
                {
                    this.eccentricAnomaly = this.CalculateEccentricAnomalyAtMeanAnomaly(
                        this.MeanAnomaly
                    );
                }
                else if (this.calculatedTrueAnomaly)
                {
                    throw new NotImplementedException(
                        "Cannot calculate eccentric anomaly from true anomaly"
                    );
                }
                else
                {
                    throw new NotSupportedException("Missing data to calculate eccentric anomaly");
                }

                this.calculatedEccentricAnomaly = true;
            }

            return this.eccentricAnomaly;
        }
    }

    private bool calculatedTrueAnomaly = false;
    private double trueAnomaly = 0;
    public double TrueAnomaly
    {
        get
        {
            if (!this.calculatedTrueAnomaly)
            {
                if (
                    this.calculatedTime
                    || this.calculatedMeanAnomaly
                    || this.calculatedEccentricAnomaly
                )
                {
                    this.trueAnomaly = this.CalculateTrueAnomalyAtEccentricAnomaly(
                        this.EccentricAnomaly
                    );
                }
                else
                {
                    throw new NotSupportedException("Missing data to calculate eccentric anomaly");
                }

                this.calculatedEccentricAnomaly = true;
            }

            return this.trueAnomaly;
        }
    }

    // TODO lazy calculation of position and velocity

    public OrbitalState() { }

    private OrbitalState(OrbitalParameters orbitalParameters)
    {
        this.orbitalParameters = orbitalParameters;
    }

    public static OrbitalState AtTime(double time, OrbitalParameters orbitalParameters)
    {
        var result = new OrbitalState(orbitalParameters);
        result.time = time;
        result.calculatedTime = true;
        return result;
    }

    private double CalculateMeanAnomalyAtTime(double time)
    {
        return (time + this.orbitalParameters.Epoch)
            * Math.Sqrt(
                this.orbitalParameters.CentralBody.Mass
                    / (
                        this.orbitalParameters.SemiMajorAxis
                        * this.orbitalParameters.SemiMajorAxis
                        * this.orbitalParameters.SemiMajorAxis
                    )
            );
    }

    private double CalculateEccentricAnomalyAtMeanAnomaly(double meanAnomaly)
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
            this.orbitalParameters.Eccentricity > 0.8 ? Math.PI : clampedMeanAnomaly;
        int iterations = 0;

        do
        {
            prevEccentricAnomaly = eccentricAnomaly;
            eccentricAnomaly =
                eccentricAnomaly
                - (
                    (
                        eccentricAnomaly
                        - this.orbitalParameters.Eccentricity * Math.Sin(eccentricAnomaly)
                        - clampedMeanAnomaly
                    ) / (1 - this.orbitalParameters.Eccentricity * Math.Cos(eccentricAnomaly))
                );
            prevDifference = difference;
            difference = Math.Abs(eccentricAnomaly - prevEccentricAnomaly);
            iterations++;
        } while (Math.Abs(difference - prevDifference) > 1e-12 && iterations < 100);

        return this.ConformRadianMultiple(eccentricAnomaly, meanAnomaly);
    }

    private double CalculateTrueAnomalyAtEccentricAnomaly(double eccentricAnomaly)
    {
        double trueAnomaly =
            Math.Atan2(
                Math.Sqrt(1 + this.orbitalParameters.Eccentricity) * Math.Sin(eccentricAnomaly / 2),
                Math.Sqrt(1 - this.orbitalParameters.Eccentricity) * Math.Cos(eccentricAnomaly / 2)
            ) * 2;
        return this.ConformRadianMultiple(trueAnomaly, eccentricAnomaly);
    }

    private double ConformRadianMultiple(double sourceRadians, double targetRadians)
    {
        return ((sourceRadians + (Math.PI * 2)) % (Math.PI * 2))
            + (targetRadians - (targetRadians % (Math.PI * 2)));
    }
}
