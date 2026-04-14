using Godot;
using System;

public struct OrbitalParameters
{
    public CelestialBody CentralBody { get; set; } = null;
    public double SemiMajorAxis { get; set; } = 0;
    public double Eccentricity { get; set; } = 0;
    private double inclination = 0;
    public double Inclination
    {
        get => inclination;
        set
        {
            inclination = value;
            this.UpdateTransformationMatrix();
        }
    }
    private double argumentOfPeriapsis = 0;
    public double ArgumentOfPeriapsis
    {
        get => argumentOfPeriapsis;
        set
        {
            argumentOfPeriapsis = value;
            this.UpdateTransformationMatrix();
        }
    }
    private double longitudeOfAscendingNode = 0;
    public double LongitudeOfAscendingNode
    {
        get => longitudeOfAscendingNode;
        set
        {
            longitudeOfAscendingNode = value;
            this.UpdateTransformationMatrix();
        }
    }
    public double Epoch { get; set; } = 0;
    private double[,] matrix = new double[3, 2];

    public OrbitalParameters() { }

    public Vector3D ToInertialFrame(double x, double y)
    {
        return new Vector3D(
            x * this.matrix[0, 0] - y * this.matrix[0, 1],
            x * this.matrix[1, 0] + y * this.matrix[1, 1],
            -(x * this.matrix[2, 0] + y * this.matrix[2, 1])
        );
    }

    private void UpdateTransformationMatrix()
    {
        double sinAoP = Math.Sin(this.ArgumentOfPeriapsis);
        double cosAoP = Math.Cos(this.ArgumentOfPeriapsis);
        double sinI = Math.Sin(this.Inclination);
        double cosI = Math.Cos(this.Inclination);
        double sinLoAN = Math.Sin(this.LongitudeOfAscendingNode);
        double cosLoAN = Math.Cos(this.LongitudeOfAscendingNode);
        this.matrix = new double[,]
        {
            {
                cosAoP * cosLoAN - sinAoP * cosI * sinLoAN,
                -(sinAoP * cosLoAN + cosAoP * cosI * sinLoAN)
            },
            { sinAoP * sinI, cosAoP * sinI },
            {
                cosAoP * sinLoAN + sinAoP * cosI * cosLoAN,
                cosAoP * cosI * cosLoAN - sinAoP * sinLoAN,
            }
        };
    }
}
