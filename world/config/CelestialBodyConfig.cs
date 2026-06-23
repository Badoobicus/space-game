using System.Collections.Generic;
using Godot;

public class CelestialBodyConfig
{
    public static List<CelestialBody> CreateCelestialBodies()
    {
        List<CelestialBody> celestialBodies = [];

        var star = new CelestialBody
        {
            CelestialBodyId = "star",
            Mass = 100,
            Radius = 2,
            SoiRadius = 100,
        };
        celestialBodies.Add(star);

        var planet1 = new CelestialBody
        {
            CelestialBodyId = "planet1",
            Orbit = Orbit.FromElements(
                new EllipticalOrbitElements
                {
                    CenterBody = star,
                    SemiMajorAxis = 10,
                    Eccentricity = 0.05,
                    Inclination = 0,
                    LongitudeOfAscendingNode = 0,
                    ArgumentOfPeriapsis = 0,
                    MeanAnomalyAtEpoch = 0,
                },
                0
            ),
            Mass = 1,
            Radius = 0.25,
        };
        planet1.SoiRadius = OrbitUtils.CalculateSoiRadius(planet1);
        celestialBodies.Add(planet1);

        var moon1 = new CelestialBody
        {
            CelestialBodyId = "moon1",
            Orbit = Orbit.FromElements(
                new EllipticalOrbitElements
                {
                    CenterBody = planet1,
                    SemiMajorAxis = 1,
                    Eccentricity = 0,
                    Inclination = 0,
                    LongitudeOfAscendingNode = 0,
                    ArgumentOfPeriapsis = 0,
                    MeanAnomalyAtEpoch = 0,
                },
                0
            ),
            Mass = 0.05,
            Radius = 0.05,
        };
        moon1.SoiRadius = OrbitUtils.CalculateSoiRadius(moon1);
        celestialBodies.Add(moon1);

        var planet2 = new CelestialBody
        {
            CelestialBodyId = "planet2",
            Orbit = Orbit.FromElements(
                new EllipticalOrbitElements
                {
                    CenterBody = star,
                    SemiMajorAxis = 30,
                    Eccentricity = 0.2,
                    Inclination = Mathf.DegToRad(3),
                    LongitudeOfAscendingNode = Mathf.DegToRad(90),
                    ArgumentOfPeriapsis = Mathf.DegToRad(45),
                    MeanAnomalyAtEpoch = 0,
                },
                0
            ),
            Mass = 1,
            Radius = 0.5,
        };
        planet2.SoiRadius = OrbitUtils.CalculateSoiRadius(planet2);
        celestialBodies.Add(planet2);

        var moon2 = new CelestialBody
        {
            CelestialBodyId = "moon2",
            Orbit = Orbit.FromElements(
                new EllipticalOrbitElements
                {
                    CenterBody = planet2,
                    SemiMajorAxis = 1,
                    Eccentricity = 0,
                    Inclination = 0,
                    LongitudeOfAscendingNode = 0,
                    ArgumentOfPeriapsis = 0,
                    MeanAnomalyAtEpoch = 0,
                },
                0
            ),
            Mass = 0.05,
            Radius = 0.05,
        };
        moon2.SoiRadius = OrbitUtils.CalculateSoiRadius(moon2);
        celestialBodies.Add(moon2);

        var moon3 = new CelestialBody
        {
            CelestialBodyId = "moon3",
            Orbit = Orbit.FromElements(
                new EllipticalOrbitElements
                {
                    CenterBody = planet2,
                    SemiMajorAxis = 3,
                    Eccentricity = 0.1,
                    Inclination = Mathf.DegToRad(-5),
                    LongitudeOfAscendingNode = 0,
                    ArgumentOfPeriapsis = 0,
                    MeanAnomalyAtEpoch = 0,
                },
                0
            ),
            Mass = 0.05,
            Radius = 0.05,
        };
        moon3.SoiRadius = OrbitUtils.CalculateSoiRadius(moon3);
        celestialBodies.Add(moon3);

        return celestialBodies;
    }
}
