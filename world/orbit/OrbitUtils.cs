using Godot;
using System;

public class OrbitUtils
{
    public static double CalculateSoiRadius(CelestialBody body)
    {
        if (body.Orbit?.Body == null)
        {
            throw new ArgumentException(
                "Failed to calculate SOI radius; orbit parent cannot be null"
            );
        }

        return CalculateSoiRadius(body.Mass, body.Orbit.Body.Mass, body.Orbit.SemiMajorAxis);
    }

    public static double CalculateSoiRadius(double mass, double parentMass, double semiMajorAxis)
    {
        if (mass <= 0)
        {
            throw new ArgumentException(
                "Failed to calculate SOI radius; mass cannot be negative or zero"
            );
        }

        if (parentMass <= 0)
        {
            throw new ArgumentException(
                "Failed to calculate SOI radius; parent mass cannot be negative or zero"
            );
        }

        if (semiMajorAxis <= 0)
        {
            throw new ArgumentException(
                "Failed to calculate SOI radius; semi-major axis cannot be negative or zero"
            );
        }

        return semiMajorAxis * Math.Pow(mass / parentMass, 0.4);
    }
}
