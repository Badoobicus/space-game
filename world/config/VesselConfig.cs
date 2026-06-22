using System.Collections.Generic;

public class VesselConfig
{
    private static readonly Vessel Vessel1;

    public static List<Vessel> CreateVessels(Dictionary<string, CelestialBody> celestialBodyMap)
    {
        List<Vessel> vessels = new();

        var vessel1 = new Vessel
        {
            VesselId = "vessel1",
            Orbit = Orbit.FromElements(
                new EllipticalOrbitElements
                {
                    CenterBody = celestialBodyMap["planet1"],
                    SemiMajorAxis = 0.5,
                    Eccentricity = 0,
                    Inclination = 0,
                    LongitudeOfAscendingNode = 0,
                    ArgumentOfPeriapsis = 0,
                    MeanAnomalyAtEpoch = 0
                },
                0
            ),
        };
        vessels.Add(vessel1);

        return vessels;
    }
}
