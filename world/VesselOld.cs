using Godot;
using System;
using System.Collections.Generic;

public partial class VesselOld : Node3D
{
    public EllipticalOrbit Orbit { get; set; }

    public void Move(World world)
    {
        if (this.Orbit != null)
        {
            this.Position =
                this.Orbit.CelestialBody.GlobalPosition
                + (Vector3)this.Orbit.GetPositionAtTime(world.ElapsedTime);
        }
        else
        {
            this.Position = Vector3.Zero;
        }
    }

    public List<double> CalculateTimeOfClosestApproaches(World world, CelestialBody celestialBody)
    {
        // 32 seems to be a good fit for not missing any closest approaches. Don't lower without further testing.
        int steps = 32;

        List<double> closestApproaches = new List<double>();

        if (this.Orbit == null)
        {
            return closestApproaches;
        }

        double period = this.Orbit.Period;
        double lastDistanceVelocity = 0;
        double lowerTime = 0;
        double lowerDistanceVelocity = 0;
        double upperTime = 0;
        double upperDistanceVelocity = 0;

        double currentEccentricAnomaly = this.Orbit.GetEccentricAnomalyAtTime(world.ElapsedTime);
        double currentEccentricAnomaly2 = new EllipticalOrbit(
            celestialBody.Orbit.Current.OrbitalParameters
        ).GetEccentricAnomalyAtTime(world.ElapsedTime);
        double currentTrueAnomaly = this.Orbit.GetTrueAnomalyAtTime(world.ElapsedTime);
        double currentTrueAnomaly2 = new EllipticalOrbit(
            celestialBody.Orbit.Current.OrbitalParameters
        ).GetTrueAnomalyAtTime(world.ElapsedTime);

        double currentTime = world.ElapsedTime;
        double finalTime = currentTime + this.Orbit.Period;
        List<double> timesToCheck = new List<double>();
        timesToCheck.Add(currentTime);
        timesToCheck.Add(finalTime);

        double stepRadians = (Math.PI * 2) / steps;

        // Split vessel body orbit into steps.
        {
            int i = 0;
            double startTrueAnomaly = Math.Ceiling(currentTrueAnomaly / stepRadians) * stepRadians;
            while (true)
            {
                // Add small offset to ensure we don't accidentally wrap back to the previous loop at a multiple of 2pi.
                double time = this.Orbit.GetTimeAtTrueAnomaly(
                    startTrueAnomaly + i * stepRadians + 1e-4
                );
                if (time > finalTime)
                    break;
                timesToCheck.Add(time);
                i++;
            }
        }

        // Split celestial body orbit into steps. If period is shorter than period of vessel,
        // keep looping until enough time passes that the vessel has completed one full orbit.
        // TODO if the vessel has a really long period and the body has a really short one,
        // this is will be extremely slow.
        {
            int i = 0;
            double startTrueAnomaly = Math.Ceiling(currentTrueAnomaly2 / stepRadians) * stepRadians;
            if (startTrueAnomaly < currentTime) { }
            while (true)
            {
                // Add small offset to ensure we don't accidentally wrap back to the previous loop at a multiple of 2pi.
                double time = new EllipticalOrbit(
                    celestialBody.Orbit.Current.OrbitalParameters
                ).GetTimeAtTrueAnomaly(startTrueAnomaly + i * stepRadians + 1e-4);
                if (time > finalTime)
                    break;
                timesToCheck.Add(time);
                i++;
            }
        }

        timesToCheck.Sort();

        for (int i = 0; i < timesToCheck.Count; i++)
        {
            double time = timesToCheck[i];
            double distanceVelocity = this.CalculateRelativeDistanceVelocityAtTime(
                celestialBody,
                time
            );

            if ((lastDistanceVelocity < 0 && distanceVelocity > 0))
            {
                lowerTime = timesToCheck[i - 1];
                lowerDistanceVelocity = this.CalculateRelativeDistanceVelocityAtTime(
                    celestialBody,
                    lowerTime
                );
                upperTime = timesToCheck[i];
                upperDistanceVelocity = this.CalculateRelativeDistanceVelocityAtTime(
                    celestialBody,
                    upperTime
                );

                double approachTime = this.CalculateTimeOfClosestApproach(
                    lowerTime,
                    lowerDistanceVelocity,
                    upperTime,
                    upperDistanceVelocity,
                    celestialBody
                );

                lowerTime = (approachTime - (period / (steps * 2)));
                lowerDistanceVelocity = this.CalculateRelativeDistanceVelocityAtTime(
                    celestialBody,
                    lowerTime
                );
                upperTime = (approachTime + (period / (steps * 2)));
                upperDistanceVelocity = this.CalculateRelativeDistanceVelocityAtTime(
                    celestialBody,
                    upperTime
                );

                closestApproaches.Add(approachTime);
            }

            lastDistanceVelocity = distanceVelocity;
        }

        return closestApproaches;
    }

    private double CalculateTimeOfClosestApproach(
        double lowerTime,
        double lowerDistanceVelocity,
        double upperTime,
        double upperDistanceVelocity,
        CelestialBody celestialBody
    )
    {
        int iterations = 0;
        while (upperTime - lowerTime > 1e-4)
        {
            // Approximate where the velocity will be 0 by assuming constant acceleration.
            double t = -lowerDistanceVelocity / (upperDistanceVelocity - lowerDistanceVelocity);
            // If the guess is too accurate then we might end up with one of the endpoints being right on
            // the true result, which can actually cause problems.
            t = ((t - 0.5) * 0.99) + 0.5;
            if (t < 0 || t > 1 || iterations > 100)
            {
                GD.PrintErr("t:   " + t);
                GD.PrintErr("Lt : " + lowerTime);
                GD.PrintErr("Ut : " + upperTime);
                GD.PrintErr("tdf: " + (upperTime - lowerTime));
                GD.PrintErr("LdV: " + lowerDistanceVelocity);
                GD.PrintErr("UdV: " + upperDistanceVelocity);
                break;
            }
            double time = lowerTime * (1 - t) + upperTime * t;
            // TODO the distance velocity becomes a poor measure of closest approach when the distance velocity
            // is really really small (objects are approaching each other at less than one-trillionth of a m/s).
            // Investigate? Or maybe don't give really light objects a really big SOI?
            double distanceVelocity = this.CalculateRelativeDistanceVelocityAtTime(
                celestialBody,
                time
            );

            if (distanceVelocity > 0)
            {
                upperTime = time;
                upperDistanceVelocity = distanceVelocity;
            }
            else
            {
                lowerTime = time;
                lowerDistanceVelocity = distanceVelocity;
            }
            iterations++;
        }

        return (upperTime + lowerTime) / 2;
    }

    public double CalculateRelativeDistanceVelocityAtTime(CelestialBody celestialBody, double time)
    {
        Vector3D position = this.Orbit.GetPositionAtTime(time);
        Vector3D velocity = this.Orbit.GetVelocityAtTime(time);
        OrbitalState bodyState = celestialBody.Orbit.GetStateAtTime(time);
        Vector3D relativePosition = position - bodyState.Position;
        Vector3D relativeVelocity = velocity - bodyState.Velocity;
        return relativeVelocity.Project(relativePosition).Length()
            * Math.Sign(relativeVelocity.Dot(relativePosition));
    }
}
