using Godot;
using System;
using System.Collections.Generic;

public class ChebychevCalculator
{
    public static List<Func<double, double>> CreateApproximation(Func<double, double> func)
    {
        double a = -1;
        double b = 1;

        int n = 10;

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
                sum += T(k, uValues[i]) * func(xValues[i]);
            }
            coefficients[k] = (k == 0 ? 1 : 2) * sum / n;
        }

        var polynomialCoeffs = Convert(coefficients);

        var result = new List<Func<double, double>>();
        result.Add(
            (double x) =>
            {
                double result = 0;
                for (int i = 0; i < coefficients.Length; i++)
                {
                    result += coefficients[i] * T(i, x);
                }
                return result;
            }
        );
        result.Add(
            (double x) =>
            {
                double result = 0;
                for (int i = 0; i < coefficients.Length; i++)
                {
                    result += coefficients[i] * Q(i, x);
                }
                return result / 20;
            }
        );
        // result.Add(
        //     (double x) =>
        //     {
        //         double result = 0;
        //         for (int i = 0; i < polynomialCoeffs.Length; i++)
        //         {
        //             double k = polynomialCoeffs[i];
        //             for (int j = 0; j < i; j++)
        //             {
        //                 k *= x;
        //             }
        //             result += k;
        //         }
        //         return result;
        //     }
        // );
        return result;
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

    // https://scicomp.stackexchange.com/questions/28309/derivatives-of-a-chebychev-polynomial
    private static double Q(int n, double x)
    {
        double[] values = new double[n + 3];
        values[0] = 1;
        values[1] = x;

        double[] dValues = new double[n + 3];
        dValues[0] = 0;
        dValues[1] = 1;
        for (int i = 2; i <= n; i++)
        {
            values[i] = 2 * x * values[i - 1] - values[i - 2];
            dValues[i] = 2 * values[i - 1] + 2 * x * dValues[i - 1] - dValues[i - 2];
        }
        return dValues[n];
    }

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
