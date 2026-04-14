using Godot;
using System;

public abstract class OrbitSegment
{
    public OrbitalParameters OrbitalParameters { get; private set; }

    public OrbitSegment(OrbitalParameters parameters)
    {
        this.OrbitalParameters = parameters;
    }

    public static OrbitSegment FromParameters(OrbitalParameters parameters)
    {
        // TODO actually call this method and use it to replace current orbit functionality in simulation

        OrbitSegment result;
        double eccentricity = parameters.Eccentricity;
        if (eccentricity >= 0 && eccentricity < 1)
        {
            result = new EllipticalOrbitSegment(parameters);
        }
        else if (eccentricity > 1)
        {
            result = new HyperbolicOrbitSegment(parameters);
        }
        else
        {
            throw new ArgumentException($"Invalid eccentricity: {eccentricity}");
        }
        return result;
    }

    public virtual double GetMeanAnomalyAtTime(double time)
    {
        return (time + this.OrbitalParameters.Epoch)
            * Math.Sqrt(
                this.OrbitalParameters.CentralBody.Mass
                    / (
                        this.OrbitalParameters.SemiMajorAxis
                        * this.OrbitalParameters.SemiMajorAxis
                        * this.OrbitalParameters.SemiMajorAxis
                    )
            );
    }

    public abstract OrbitalState GetStateAtTime(double time);
}
