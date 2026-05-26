using Godot;
using System;

public class OrbitSolver
{
    public double Period { get; }
    public double Periapsis { get; }
    public double Apoapsis { get; }

    /// M - mass of main body
    private readonly double _M;

    /// r_0 - initial position
    private readonly Vector3 _r0;

    /// ||r_0|| - magnitude of initial position
    private readonly double _r0Mag;

    /// v_0 - initial velocity
    private readonly Vector3 _v0;

    /// ||v_0||^2 - squared magnitude of initial velocity
    private readonly double _v0MagSq;

    /// r_0.v_0 - dot product of intial position and velocity
    private readonly double _r0DotV0;

    /// mu - mass of main body scaled by gravitation constant
    private readonly double _mu;

    /// sqrt(mu) - square root of mu
    private readonly double _sqrtMu;

    /// alpha - energy of orbit
    private readonly double _alpha;

    // sqrtAlpha - square root of energy of orbit
    private readonly double _sqrtAlpha;

    /// t0 - epoch
    private readonly double _t0;

    private OrbitSolver(
        double mainBodyMass,
        Vector3 initialPosition,
        Vector3 initialVelocity,
        double epoch
    )
    {
        _M = mainBodyMass;
        _r0 = initialPosition;
        _r0Mag = _r0.Length();
        _v0 = initialVelocity;
        _v0MagSq = _v0.LengthSquared();
        _r0DotV0 = _r0.Dot(_v0);
        _mu = _M * Constants.GravitationalConstant;
        _sqrtMu = Math.Sqrt(_mu);
        _alpha = 2 / _r0Mag - _v0MagSq / _mu;
        _sqrtAlpha = Math.Sqrt(_alpha);
        _t0 = epoch;

        var a = 1 / _alpha;
        Period = Math.PI * 2 * a * Math.Sqrt(a / _mu);

        // TODO convert to Vector3d (and all other orbital math to use doubles across the board)
        Vector3 eccentricityVector =
            ((float)(_v0MagSq - (_mu / _r0Mag)) * _r0 - (float)_r0DotV0 * _v0) / (float)_mu;
        double e = eccentricityVector.Length();

        if (e < 1)
        {
            Periapsis = a * (1 - e);
            Apoapsis = a * (1 + e);
        }
        else
        {
            Periapsis = a * (1 - e);
            Apoapsis = -1;
        }
    }

    public (Vector3, Vector3) SolveStateAtTime(double time)
    {
        double chi = _SolveUniversalAnomalyAtTime(time);
        return _SolveStateAtUniversalAnomaly(chi);
    }

    public (Vector3, Vector3) SolveStateAtEccentricAnomaly(double eccentricAnomaly)
    {
        return _SolveStateAtUniversalAnomaly(eccentricAnomaly / _sqrtAlpha);
    }

    public double SolveTimeAtRadius(double radius)
    {
        double universalAnomaly = _SolveUniversalAnomalyAtRadius(radius);
        return _SolveTimeAtUniversalAnomaly(universalAnomaly);
    }

    private double _SolveUniversalAnomalyAtTime(double time)
    {
        double delta = time - _t0;

        // initial guess for chi
        double chi = _sqrtMu * delta * Mathf.Abs(_alpha);
        if (_alpha < 1e-6)
            chi = _sqrtMu * delta / _r0Mag;

        // Newton-Raphson
        int maxIterations = 100;
        for (int i = 0; i < maxIterations; i++)
        {
            double z = _alpha * chi * chi;
            double c2 = StumpffC2(z);
            double c3 = StumpffC3(z);

            double fChi =
                (_r0DotV0 / _sqrtMu) * chi * chi * c2
                + (1 - _alpha * _r0Mag) * chi * chi * chi * c3
                + _r0Mag * chi
                - (_sqrtMu * delta);

            double dfChi =
                chi * chi * c2
                + (_r0DotV0 / _sqrtMu) * chi * (1.0 - z * c3)
                + _r0Mag * (1.0 - z * c2);

            double chiNext = chi - (fChi / dfChi);

            if (Mathf.Abs(chiNext - chi) < 1e-8)
            {
                chi = chiNext;
                break;
            }

            chi = chiNext;
        }

        return chi;
    }

    private (Vector3, Vector3) _SolveStateAtUniversalAnomaly(double chi)
    {
        double z = _alpha * chi * chi;
        double chiSq = chi * chi;

        double c2 = StumpffC2(z);
        double c3 = StumpffC3(z);

        double f = 1 - (chiSq / _r0Mag) * c2;
        double g = _r0Mag * chi * (1 - z * c3) / _sqrtMu + _r0DotV0 * chiSq * c2 / _mu;

        // calculate final position
        Vector3 rNext = (float)f * _r0 + (float)g * _v0;
        double rNextMag = rNext.Length();

        // calculate f-dot and g-dot
        double fDot = (_sqrtMu / (rNextMag * _r0Mag)) * chi * (z * c3 - 1);
        double gDot = 1 - (chiSq / rNextMag) * c2;

        // calculate final velocity
        Vector3 vNext = (float)fDot * _r0 + (float)gDot * _v0;

        return (rNext, vNext);
    }

    private double _SolveUniversalAnomalyAtRadius(double radius)
    {
        double chi = 0.0;

        // Newton-Raphson
        int maxIterations = 100;
        for (int i = 0; i < maxIterations; i++)
        {
            double z = _alpha * chi * chi;
            double c2 = StumpffC2(z);
            double c3 = StumpffC3(z);

            double rChi =
                (_r0DotV0 / _sqrtMu) * chi * (1 - z * c3)
                + (1 - _alpha * _r0Mag) * chi * chi * c2
                + _r0Mag;

            double drChi =
                (_r0DotV0 / _sqrtMu) * (1 - z * c2)
                + (1 - _alpha * _r0Mag) * chi * (1 - z * c3)
                + _r0Mag;

            // Prevent division by zero if we hit an exact apoapsis/periapsis turn
            if (Math.Abs(drChi) < 1e-12)
            {
                drChi = drChi < 0 ? -1e-12 : 1e-12;
            }

            double fChi = rChi - radius;
            double chiNext = chi - (fChi / drChi);

            if (Math.Abs(chiNext - chi) < 1e-8)
            {
                chi = chiNext;
                break;
            }

            chi = chiNext;
        }

        return chi;
    }

    private double _SolveTimeAtUniversalAnomaly(double chi)
    {
        double z = _alpha * chi * chi;
        double c2 = StumpffC2(z);
        double c3 = StumpffC3(z);

        double dt =
            (
                (_r0DotV0 / _sqrtMu) * chi * chi * c2
                + (1 - _alpha * _r0Mag) * chi * chi * chi * c3
                + _r0Mag * chi
            ) / _sqrtMu;

        return _t0 + dt;
    }

    private double StumpffC2(double z)
    {
        double result;

        if (z > 0.1)
        {
            return (1 - Math.Cos(Math.Sqrt(z))) / z;
        }

        if (z < -0.1)
        {
            return (Math.Cosh(Math.Sqrt(-z)) - 1) / -z;
        }

        double zSqr = z * z;
        return 1.0 / 2.0 - z / 24.0 + zSqr / 720.0 - z * zSqr / 40320.0 + zSqr * zSqr / 3628800.0;
    }

    private double StumpffC3(double z)
    {
        if (z > 0.1)
        {
            var sqrtZ = Math.Sqrt(z);
            return (sqrtZ - Math.Sin(sqrtZ)) / (z * sqrtZ);
        }

        if (z < -0.1)
        {
            var negZ = -z;
            var sqrtNegZ = Math.Sqrt(-z);
            return (Math.Sinh(sqrtNegZ) - sqrtNegZ) / (negZ * sqrtNegZ);
        }

        double zSqr = z * z;
        return 1.0 / 6.0
            - z / 120.0
            + zSqr / 5040.0
            - z * zSqr / 362880.0
            + zSqr * zSqr / 39916800.0;
    }

    public static OrbitSolver FromInitialState(
        double mainBodyMass,
        Vector3 initialPosition,
        Vector3 initialVelocity,
        double epoch
    )
    {
        return new OrbitSolver(mainBodyMass, initialPosition, initialVelocity, epoch);
    }
}
