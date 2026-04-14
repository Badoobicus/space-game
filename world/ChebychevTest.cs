using Godot;
using System;
using MathNet.Numerics.LinearAlgebra;

public class ChebychevTest
{
    public static void Approximate(EllipticalOrbit orbit1, EllipticalOrbit orbit2, double time)
    {
        // https://www.embeddedrelated.com/showarticle/152.php

        double start = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        var myFunction = (double t) => Math.Sin(Math.PI * t); // orbit1.GetPositionAtTime(t).DistanceTo(orbit2.GetPositionAtTime(t)) - 1;

        double a = -0.5;
        double b = 0.5;

        int n = 5;

        double[] uValues = new double[n];
        for (int i = 1; i <= n; i++)
        {
            uValues[i - 1] = Math.Cos(Math.PI * (2 * i - 1) / (2 * n));
        }

        double[] xValues = new double[n];
        for (int i = 0; i < n; i++)
        {
            xValues[i] = ((b - a) / 2) * uValues[i] + (a + b) / 2;
        }

        double[] coefficients = new double[n];
        for (int k = 0; k < n; k++)
        {
            double sum = 0;
            for (int i = 0; i < n; i++)
            {
                sum += T(k, uValues[i]) * myFunction(xValues[i]);
            }
            coefficients[k] = (k == 0 ? 1 : 2) * sum / n;
        }

        // Everything above this point works for calculating Chebychev coefficients.
        // Everything below - not guaranteed

        double[,] testArray = new double[n, n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (j == n - 1)
                {
                    testArray[i, j] = coefficients[i];
                }
                else if (i - 1 == j)
                {
                    testArray[i, j] = 1;
                }
                else
                {
                    testArray[i, j] = 0;
                }
            }
        }

        double[] monicPolynomialCoefficients = Convert(coefficients);
        GD.Print("cheb c: " + coefficients.Join(", "));
        GD.Print("poly c: " + monicPolynomialCoefficients.Join(", "));

        Matrix<double> companionMatrix = Matrix<double>.Build.DenseOfArray(testArray);

        foreach (var eigenValue in companionMatrix.Evd().EigenValues)
        {
            if (eigenValue.Imaginary == 0)
            {
                double u = eigenValue.Real;
                GD.Print("time to intercept: " + (((b - a) / 2) * u) + ((a + b) / 2));
            }
        }

        double end = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        GD.Print("ms: " + (end - start));
    }

    private static double T(int n, double x)
    {
        double[] values = new double[n + 3];
        values[0] = 1;
        values[1] = x;
        for (int i = 2; i <= n; i++)
        {
            values[i] = 2 * x * values[i - 1] - values[i - 2];
        }
        return values[n];
    }

    // TODO I don't think this method works, redo
    public static double[] Convert(double[] chebyshevCoeffs)
    {
        // Validate input
        if (chebyshevCoeffs.Length == 0)
        {
            throw new ArgumentException("Chebyshev polynomial must have at least one coefficient.");
        }

        int n = chebyshevCoeffs.Length;
        double[] polynomialCoeffs = new double[n];

        // Handle constant and linear terms separately
        polynomialCoeffs[0] = chebyshevCoeffs[n - 1];
        if (n > 1)
        {
            polynomialCoeffs[1] = chebyshevCoeffs[n - 2];
        }

        // Apply Clenshaw algorithm for remaining coefficients
        double a_i = 2 * chebyshevCoeffs[n - 1];
        double b_i = chebyshevCoeffs[n - 2];
        for (int i = 2; i < n; i++)
        {
            polynomialCoeffs[i] = 2 * a_i - b_i;
            b_i = a_i;
            a_i = 2 * chebyshevCoeffs[n - i - 1] - b_i;
        }

        return polynomialCoeffs;
    }
}
