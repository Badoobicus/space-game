using Godot;
using System;

public class EllipticalOrbitSolver
{
    // Keplerian orbit elements => Cartesian state vectors
    // See https://downloads.rene-schwarz.com/download/M001-Keplerian_Orbit_Elements_to_Cartesian_State_Vectors.pdf

    // Cartesian state vectors => Keplerian orbit elements
    // See https://downloads.rene-schwarz.com/download/M002-Cartesian_State_Vectors_to_Keplerian_Orbit_Elements.pdf

    private const double TwoPi = Math.PI * 2;

    public static StateVector SolveState(EllipticalOrbitElements elements, double time)
    {
        double a = elements.SemiMajorAxis;
        double e = elements.Eccentricity;
        double M0 = elements.MeanAnomalyAtEpoch;
        double u = elements.CenterBody.Mass * Constants.GravitationalConstant;

        double dt = time;
        double M = WrapAngle(M0 + dt * (1 / a) * Math.Sqrt(u / a));

        double E = e > 0.8 ? Math.PI : M;

        for (int j = 0; j <= 10; j++)
        {
            double prevE = E;
            E -= (E - e * Math.Sin(E) - M) / (1 - e * Math.Cos(E));

            if (Math.Abs(E - prevE) < 1e-12)
            {
                break;
            }
        }

        double v =
            2 * Math.Atan2(Math.Sqrt(1 + e) * Math.Sin(E / 2), Math.Sqrt(1 - e) * Math.Cos(E / 2));

        return SolveStateFromAnomalies(elements, E, v);
    }

    public static StateVector SolveStateFromTrueAnomaly(
        EllipticalOrbitElements orbit,
        double trueAnomaly
    )
    {
        double e = orbit.Eccentricity;
        double v = WrapAngle(trueAnomaly);

        double E =
            Math.Atan2(Math.Sqrt(1 - e) * Math.Sin(v / 2), Math.Sqrt(1 + e) * Math.Cos(v / 2)) * 2;

        return SolveStateFromAnomalies(orbit, E, v);
    }

    private static StateVector SolveStateFromAnomalies(
        EllipticalOrbitElements orbit,
        double eccentricAnomaly,
        double trueAnomaly
    )
    {
        double a = orbit.SemiMajorAxis;
        double e = orbit.Eccentricity;
        double u = orbit.CenterBody.Mass * Constants.GravitationalConstant;
        double E = eccentricAnomaly;
        double v = trueAnomaly;

        double r = a * (1 - e * Math.Cos(E));

        double px = r * Math.Cos(v);
        double py = r * Math.Sin(v);

        double vs = Math.Sqrt(u * a) / r;
        double vx = vs * -Math.Sin(E);
        double vy = vs * Math.Sqrt(1 - e * e) * Math.Cos(E);

        Vector3d position = OrbitalToInertial(px, py, orbit);
        Vector3d velocity = OrbitalToInertial(vx, vy, orbit);

        return new StateVector(position, velocity);
    }

    private static double WrapAngle(double radians)
    {
        return ((radians % TwoPi) + TwoPi) % TwoPi;
    }

    private static Vector3d OrbitalToInertial(double x, double y, EllipticalOrbitElements orbit)
    {
        double lan = orbit.LongitudeOfAscendingNode;
        double inc = orbit.Inclination;
        double argPeri = orbit.ArgumentOfPeriapsis;

        double cosL = Math.Cos(lan);
        double sinL = Math.Sin(lan);
        double cosI = Math.Cos(inc);
        double sinI = Math.Sin(inc);
        double cosA = Math.Cos(argPeri);
        double sinA = Math.Sin(argPeri);

        double r11 = cosL * cosA - sinL * cosI * sinA;
        double r12 = -cosL * sinA - sinL * cosI * cosA;

        double r21 = sinL * cosA + cosL * cosI * sinA;
        double r22 = -sinL * sinA + cosL * cosI * cosA;

        double r31 = sinI * sinA;
        double r32 = sinI * cosA;

        double xInertial = x * r11 + y * r12;
        double yInertial = x * r21 + y * r22;
        double zInertial = x * r31 + y * r32;

        return new Vector3d(xInertial, zInertial, yInertial);
    }
}
