using System;

public class HyperbolicOrbit
{
    public CelestialBody CelestialBody { get; set; }
    public double SemiMajorAxis { get; set; }
    public double Eccentricity { get; set; }
    public double Inclination { get; set; }
    public double ArgumentOfPeriapsis { get; set; }
    public double LongitudeOfAscendingNode { get; set; }

    public Vector3D GetPositionAtTime(double time)
    {
        double meanAnomaly =
            time
            * Math.Sqrt(
                this.CelestialBody.Mass
                    / (this.SemiMajorAxis * this.SemiMajorAxis * this.SemiMajorAxis)
            );
        double hyperbolicAnomaly = this.GetHyperbolicAnomalyAtMeanAnomaly(meanAnomaly);
        double trueAnomaly =
            Math.Atan2(
                (Math.Sqrt(this.Eccentricity + 1) * Math.Sinh(hyperbolicAnomaly / 2)),
                (Math.Sqrt(this.Eccentricity - 1) * Math.Cosh(hyperbolicAnomaly / 2))
            ) * 2;

        double distance =
            -this.SemiMajorAxis * (1 - this.Eccentricity * Math.Cosh(hyperbolicAnomaly));
        double orbitalFramePositionX = distance * Math.Cos(trueAnomaly);
        double orbitalFramePositionY = distance * Math.Sin(trueAnomaly);
        double sinAoP = Math.Sin(this.ArgumentOfPeriapsis);
        double cosAoP = Math.Cos(this.ArgumentOfPeriapsis);
        double sinI = Math.Sin(this.Inclination);
        double cosI = Math.Cos(this.Inclination);
        double sinLoAN = Math.Sin(this.LongitudeOfAscendingNode);
        double cosLoAN = Math.Cos(this.LongitudeOfAscendingNode);

        return new Vector3D(
            orbitalFramePositionX * (cosAoP * cosLoAN - sinAoP * cosI * sinLoAN)
                - orbitalFramePositionY * (sinAoP * cosLoAN + cosAoP * cosI * sinLoAN),
            orbitalFramePositionX * (sinAoP * sinI) + orbitalFramePositionY * (cosAoP * sinI),
            -(
                orbitalFramePositionX * (cosAoP * sinLoAN + sinAoP * cosI * cosLoAN)
                + orbitalFramePositionY * (-sinAoP * sinLoAN + cosAoP * cosI * cosLoAN)
            )
        );
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
        double hyperbolicAnomaly = Math.Asinh(meanAnomaly / this.Eccentricity);
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
                        this.Eccentricity * Math.Sinh(hyperbolicAnomaly)
                        - hyperbolicAnomaly
                        - meanAnomaly
                    ) / (this.Eccentricity * Math.Cosh(hyperbolicAnomaly) - 1)
                );
            prevDifference = difference;
            difference = Math.Abs(hyperbolicAnomaly - prevHyperbolicAnomaly);
            i++;
        } while ((Math.Abs(difference - prevDifference) > 1e-16) && i < 100);

        return hyperbolicAnomaly;
    }

    public static HyperbolicOrbit FromPositionAndVelocity(
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

        HyperbolicOrbit orbit = new HyperbolicOrbit
        {
            CelestialBody = celestialBody,
            SemiMajorAxis =
                -1 / ((2 / position.Length()) - (velocity.LengthSquared() / celestialBody.Mass)),
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

        return orbit;
    }
}
