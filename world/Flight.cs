using Godot;
using System;

public partial class Flight : Node3D
{
    [Export]
    private double semiMajorAxis = 2;

    [Export]
    private double eccentricity = 0;

    [Export]
    private double argumentOfPeriapsis = Mathf.DegToRad(-90);

    [Export]
    private double longitudeOfAscendingNode = Mathf.DegToRad(0);

    [Export]
    private double inclination = Mathf.DegToRad(0);

    private readonly double startTime = Time.GetUnixTimeFromSystem();

    public override void _PhysicsProcess(double delta)
    {
        // See https://downloads.rene-schwarz.com/download/M001-Keplerian_Orbit_Elements_to_Cartesian_State_Vectors.pdf

        double centralBodyMass = 25;

        double timeElapsed = Time.GetUnixTimeFromSystem() - startTime;

        double meanAnomaly =
            (
                timeElapsed
                * Math.Sqrt(centralBodyMass / (semiMajorAxis * semiMajorAxis * semiMajorAxis))
            ) % (Math.PI * 2);

        double prevEccentricAnomaly;
        double eccentricAnomaly = eccentricity > 0.8 ? Math.PI : meanAnomaly;
        do
        {
            prevEccentricAnomaly = eccentricAnomaly;
            eccentricAnomaly =
                eccentricAnomaly
                - (eccentricAnomaly - eccentricity * Math.Sin(eccentricAnomaly) - meanAnomaly)
                    / (1 - eccentricity * Math.Cos(eccentricAnomaly));
        } while (eccentricAnomaly - prevEccentricAnomaly > 1e-16);

        double trueAnomaly =
            Math.Atan2(
                Math.Sqrt(1 + eccentricity) * Math.Sin(eccentricAnomaly / 2),
                Math.Sqrt(1 - eccentricity) * Math.Cos(eccentricAnomaly / 2)
            ) * 2;

        double distance = semiMajorAxis * (1 - eccentricity * Math.Cos(eccentricAnomaly));
        Vector2 orbitalFramePosition =
            new Vector2((float)Math.Cos(trueAnomaly), (float)Math.Sin(trueAnomaly))
            * (float)distance;

        double sinAoP = Math.Sin(argumentOfPeriapsis);
        double cosAoP = Math.Cos(argumentOfPeriapsis);
        double sinI = Math.Sin(inclination);
        double cosI = Math.Cos(inclination);
        double sinLoAN = Math.Sin(longitudeOfAscendingNode);
        double cosLoAN = Math.Cos(longitudeOfAscendingNode);
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

        this.Position = inertialFramePosition;
    }
}
