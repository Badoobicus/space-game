using Godot;
using System;

public class OldOrbitSegment
{
    public OrbitalParameters OrbitalParameters { get; private set; }
    public double Period { get; private set; }

    public OldOrbitSegment(OrbitalParameters parameters)
    {
        this.OrbitalParameters = parameters;
        this.Period =
            parameters.Eccentricity < 1
                ? (Math.PI * 2)
                    * Math.Sqrt(
                        (
                            this.OrbitalParameters.SemiMajorAxis
                            * this.OrbitalParameters.SemiMajorAxis
                            * this.OrbitalParameters.SemiMajorAxis
                        ) / this.OrbitalParameters.CentralBody.Mass
                    )
                : -1;
    }

    public OrbitalState GetStateAtTime(double time)
    {
        double meanAnomaly =
            (time + this.OrbitalParameters.Epoch)
            * Math.Sqrt(
                this.OrbitalParameters.CentralBody.Mass
                    / (
                        this.OrbitalParameters.SemiMajorAxis
                        * this.OrbitalParameters.SemiMajorAxis
                        * this.OrbitalParameters.SemiMajorAxis
                    )
            );

        if (this.OrbitalParameters.Eccentricity < 1)
        {
            double eccentricAnomaly = this.CalculateEccentricAnaomalyAtMeanAnomaly(meanAnomaly);
            double trueAnomaly = this.CalculateTrueAnomalyAtEccentricAnomaly(eccentricAnomaly);
        }
        else if (this.OrbitalParameters.Eccentricity > 1)
        {
            double hyperbolicAnomaly = this.CalculateHyperbolicAnomalyAtMeanAnomaly(meanAnomaly);
            double trueAnomaly = this.CalculateTrueAnomalyAtHypberbolicAnomaly(hyperbolicAnomaly);
        }

        // TODO complete calculation and return position
        return null;
    }

    private double CalculateEccentricAnaomalyAtMeanAnomaly(double meanAnomaly)
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

    private double CalculateTrueAnomalyAtEccentricAnomaly(double eccentricAnomaly)
    {
        double trueAnomaly =
            Math.Atan2(
                Math.Sqrt(1 + this.OrbitalParameters.Eccentricity) * Math.Sin(eccentricAnomaly / 2),
                Math.Sqrt(1 - this.OrbitalParameters.Eccentricity) * Math.Cos(eccentricAnomaly / 2)
            ) * 2;
        return this.ConformRadianMultiple(trueAnomaly, eccentricAnomaly);
    }

    private double CalculateHyperbolicAnomalyAtMeanAnomaly(double meanAnomaly)
    {
        // Repeatedly improve estimation of eccentric anomaly until estimates stop improving, which
        // happens when the difference between estimates stops decreasing, at which point we have reached
        // the best estimate a double-precision floating point calculation can handle.

        // Normally, this happens after just a single iteration for circular orbits, and around 10
        // iterations for highly elliptical orbits.

        double prevDifference;
        double difference = 10;
        double prevHyperbolicAnomaly;
        // This seems to be a good first guess which I made up.
        // Created by assuming M = e * sinh(H) and solving for H,
        // and using that as our starting approximation for  M = e * sinh(H) - H
        double hyperbolicAnomaly = Math.Asinh(meanAnomaly / this.OrbitalParameters.Eccentricity);
        int i = 0;

        // TODO this fails when evaluating orbits with eccentrieies very close to 1 (1 + ~1e-12) at mean
        // anomalies very close to 0 (~1e-12). Might be fine to leave it alone for now, but if the simulation
        // starts glitching out when ship is travelling at very close to escape velocity, this may be why.
        do
        {
            prevHyperbolicAnomaly = hyperbolicAnomaly;
            hyperbolicAnomaly =
                hyperbolicAnomaly
                - (
                    (
                        this.OrbitalParameters.Eccentricity * Math.Sinh(hyperbolicAnomaly)
                        - hyperbolicAnomaly
                        - meanAnomaly
                    ) / (this.OrbitalParameters.Eccentricity * Math.Cosh(hyperbolicAnomaly) - 1)
                );
            prevDifference = difference;
            difference = Math.Abs(hyperbolicAnomaly - prevHyperbolicAnomaly);
            i++;
        } while ((Math.Abs(difference - prevDifference) > 1e-16) && i < 100);

        return hyperbolicAnomaly;
    }

    private double CalculateTrueAnomalyAtHypberbolicAnomaly(double hyperbolicAnomaly)
    {
        return Math.Atan2(
                (
                    Math.Sqrt(this.OrbitalParameters.Eccentricity + 1)
                    * Math.Sinh(hyperbolicAnomaly / 2)
                ),
                (
                    Math.Sqrt(this.OrbitalParameters.Eccentricity - 1)
                    * Math.Cosh(hyperbolicAnomaly / 2)
                )
            ) * 2;
    }

    private double ConformRadianMultiple(double sourceRadians, double targetRadians)
    {
        return ((sourceRadians + (Math.PI * 2)) % (Math.PI * 2))
            + (targetRadians - (targetRadians % (Math.PI * 2)));
    }
}
