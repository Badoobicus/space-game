using Godot;
using System;

public partial class Orbit
{
    public CelestialBody CelestialBody { get; set; }
    public double SemiMajorAxis { get; set; }
    public double Eccentricity { get; set; }
    public double Inclination { get; set; }
    public double ArgumentOfPeriapsis { get; set; }
    public double LongitudeOfAscendingNode { get; set; }

    public Vector3 GetPositionAtTime(double time)
    {
        // See https://downloads.rene-schwarz.com/download/M001-Keplerian_Orbit_Elements_to_Cartesian_State_Vectors.pdf

        double centralBodyMass = this.CelestialBody.Mass;

        double meanAnomaly =
            (
                time
                * Math.Sqrt(
                    centralBodyMass / (this.SemiMajorAxis * this.SemiMajorAxis * this.SemiMajorAxis)
                )
            ) % (Math.PI * 2);

        double prevEccentricAnomaly;
        double eccentricAnomaly = this.Eccentricity > 0.8 ? Math.PI : meanAnomaly;
        do
        {
            prevEccentricAnomaly = eccentricAnomaly;
            eccentricAnomaly =
                eccentricAnomaly
                - (eccentricAnomaly - this.Eccentricity * Math.Sin(eccentricAnomaly) - meanAnomaly)
                    / (1 - this.Eccentricity * Math.Cos(eccentricAnomaly));
        } while (eccentricAnomaly - prevEccentricAnomaly > 1e-16);

        double trueAnomaly =
            Math.Atan2(
                Math.Sqrt(1 + this.Eccentricity) * Math.Sin(eccentricAnomaly / 2),
                Math.Sqrt(1 - this.Eccentricity) * Math.Cos(eccentricAnomaly / 2)
            ) * 2;

        double distance = this.SemiMajorAxis * (1 - this.Eccentricity * Math.Cos(eccentricAnomaly));
        Vector2 orbitalFramePosition =
            new Vector2((float)Math.Cos(trueAnomaly), (float)Math.Sin(trueAnomaly))
            * (float)distance;

        double sinAoP = Math.Sin(this.ArgumentOfPeriapsis);
        double cosAoP = Math.Cos(this.ArgumentOfPeriapsis);
        double sinI = Math.Sin(this.Inclination);
        double cosI = Math.Cos(this.Inclination);
        double sinLoAN = Math.Sin(this.LongitudeOfAscendingNode);
        double cosLoAN = Math.Cos(this.LongitudeOfAscendingNode);
        Vector3 inertialFramePosition = new Vector3(
            (
                orbitalFramePosition.X * (float)(cosAoP * cosLoAN - sinAoP * cosI * sinLoAN)
                - orbitalFramePosition.Y * (float)(sinAoP * cosLoAN + cosAoP * cosI * sinLoAN)
            ),
            orbitalFramePosition.X * (float)(sinAoP * sinI)
                + orbitalFramePosition.Y * (float)(cosAoP * sinI),
            (
                orbitalFramePosition.X * (float)(cosAoP * sinLoAN + sinAoP * cosI * cosLoAN)
                + orbitalFramePosition.Y * (float)(-sinAoP * sinLoAN + cosAoP * cosI * cosLoAN)
            )
        );

        return inertialFramePosition;
    }
}
