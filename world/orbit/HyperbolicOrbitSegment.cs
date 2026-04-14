using Godot;
using System;

public class HyperbolicOrbitSegment : OrbitSegment
{
    public HyperbolicOrbitSegment(OrbitalParameters parameters)
        : base(parameters) { }

    public override OrbitalState GetStateAtTime(double time)
    {
        double meanAnomaly = this.GetMeanAnomalyAtTime(time);
        double hyperbolicAnomaly = this.GetHyperbolicAnomalyAtMeanAnomaly(meanAnomaly);
        double trueAnomaly = this.GetTrueAnomalyAtHyperbolicAnomaly(hyperbolicAnomaly);
        return null;
    }

    public double GetHyperbolicAnomalyAtMeanAnomaly(double meanAnomaly)
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

        // TODO this fails when evaluating orbits with eccentrieies very close to 1 (1 +- ~1e-12) at mean
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

    public double GetTrueAnomalyAtHyperbolicAnomaly(double hyperbolicAnomaly)
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

    private OrbitalState GetStateAtAnomalies(double hyperbolicAnomaly, double trueAnomaly)
    {
        double distance =
            -this.OrbitalParameters.SemiMajorAxis
            * (1 - this.OrbitalParameters.Eccentricity * Math.Cosh(hyperbolicAnomaly));
        Vector3D position = this.OrbitalParameters.ToInertialFrame(
            distance * Math.Cos(trueAnomaly),
            distance * Math.Sin(trueAnomaly)
        );

        // TODO figure out how to calculate velocity of hyperbolic orbits
        Vector3D velocity = Vector3D.Zero;

        return new OrbitalState
        {
            Position = position,
            Velocity = velocity,
            CentralBody = this.OrbitalParameters.CentralBody
        };
    }
}
