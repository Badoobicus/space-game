public class Orbit
{
    public readonly CelestialBody Body;
    public readonly double SemiMajorAxis;
    public readonly double Eccentricity;
    public readonly double Inclination;
    public readonly double LongitudeOfAscendingNode;
    public readonly double ArgumentOfPeriapsis;
    public readonly double MeanAnomalyAtEpoch;

    private Orbit(
        CelestialBody body,
        double semiMajorAxis,
        double eccentricity,
        double inclination,
        double longitudeOfAscendingNode,
        double argumentOfPeriapsis,
        double meanAnomalyAtEpoch
    )
    {
        Body = body;
        SemiMajorAxis = semiMajorAxis;
        Eccentricity = eccentricity;
        Inclination = inclination;
        LongitudeOfAscendingNode = longitudeOfAscendingNode;
        ArgumentOfPeriapsis = argumentOfPeriapsis;
        MeanAnomalyAtEpoch = meanAnomalyAtEpoch;
    }

    public static Orbit FromElements(
        CelestialBody body,
        double semiMajorAxis,
        double eccentricity,
        double inclination,
        double longitudeOfAscendingNode,
        double argumentOfPeriapsis,
        double meanAnomalyAtEpoch
    )
    {
        return new Orbit(
            body,
            semiMajorAxis,
            eccentricity,
            inclination,
            longitudeOfAscendingNode,
            argumentOfPeriapsis,
            meanAnomalyAtEpoch
        );
    }
}
