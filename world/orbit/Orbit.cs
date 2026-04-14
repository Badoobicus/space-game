using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Orbit
{
    public List<OrbitSegment> Segments
    {
        get => new List<OrbitSegment>(this.segments);
    }
    public OrbitSegment Current
    {
        get => this.segments.Count > 0 ? this.segments[0] : null;
    }
    private readonly List<OrbitSegment> segments = new List<OrbitSegment>();

    public Orbit() { }

    public Orbit(OrbitalParameters parameters)
    {
        this.segments.Add(OrbitSegment.FromParameters(parameters));
    }

    public OrbitalState GetStateAtTime(double time)
    {
        if (this.segments.Count > 0)
        {
            return segments[0].GetStateAtTime(time);
        }
        else
        {
            return new OrbitalState { Position = Vector3D.Zero, Velocity = Vector3D.Zero };
        }
    }

    public void CalculateFutureSegments(World world)
    {
        var currentSegment = this.segments[this.segments.Count - 1];
        if (currentSegment is EllipticalOrbitSegment ellipticalOrbitSegment)
        {
            foreach (CelestialBody celestialBody in world.CelestialBodies)
            {
                if (
                    celestialBody.Orbit.Current.OrbitalParameters.CentralBody
                    == currentSegment.OrbitalParameters.CentralBody
                )
                {
                    double timeOfIntercept = this.CalculateTimeOfIntercept(
                        ellipticalOrbitSegment,
                        celestialBody,
                        world.ElapsedTime
                    );
                    if (timeOfIntercept > 0)
                    {
                        ellipticalOrbitSegment.GetStateAtTime(timeOfIntercept);
                        // TODO calculate orbit around intersected body
                    }
                }
            }
        }
        else
        {
            throw new NotSupportedException(
                $"Unsupported orbit segment type: {currentSegment.GetType()}"
            );
        }
    }

    private double CalculateTimeOfIntercept(
        EllipticalOrbitSegment orbitSegment,
        CelestialBody celestialBody,
        double currentTime
    )
    {
        double finalTime = currentTime + orbitSegment.Period;
        EllipticalOrbitSegment bodyOrbitSegment =
            celestialBody.Orbit.Current as EllipticalOrbitSegment;

        List<double> timesToCheck = new List<double>();
        timesToCheck.Add(currentTime);
        timesToCheck.Add(finalTime);

        timesToCheck.Sort();

        return -1;
    }
}
