using Godot;
using System;
using System.Collections.Generic;

public partial class ChebychevDrawer : Control
{
    [Export]
    private PackedScene celestialBodyPrefab;

    public override void _Draw()
    {
        var roots = new List<double>();
        foreach (var root in new MathNet.Numerics.Polynomial(1, -5, 1, 1).Roots())
        {
            if (root.Imaginary == 0)
            {
                roots.Add(root.Real);
            }
        }
        GD.Print(string.Join(", ", roots));

        CelestialBody star = this.celestialBodyPrefab.Instantiate<CelestialBody>();
        star.Radius = 5;
        star.Mass = 2000;

        EllipticalOrbit orbit1 = new EllipticalOrbit
        {
            CelestialBody = star,
            SemiMajorAxis = 15,
            Eccentricity = 0.5,
            Inclination = 0,
            ArgumentOfPeriapsis = 0,
            LongitudeOfAscendingNode = 0,
        };

        EllipticalOrbit orbit2 = new EllipticalOrbit
        {
            CelestialBody = star,
            SemiMajorAxis = 15,
            Eccentricity = 0.5,
            Inclination = 45,
            ArgumentOfPeriapsis = 0,
            LongitudeOfAscendingNode = 0,
        };

        var myFunction = (double x) =>
            (double)orbit1.GetPositionAtTime(x * 5).DistanceTo(orbit2.GetPositionAtTime(x * 5));
        var myFunctions = ChebychevCalculator.CreateApproximation(myFunction);
        myFunctions.Insert(0, myFunction);

        Vector2 size = this.GetViewportRect().Size;
        Vector2 offset = size / 2;

        for (int j = 0; j < myFunctions.Count; j++)
        {
            Color color1;
            Color color2;
            if (j == 0)
            {
                color1 = Colors.Red;
                color2 = Colors.DarkRed;
            }
            else if (j == 1)
            {
                color1 = Colors.Blue;
                color2 = Colors.Cyan;
            }
            else
            {
                color1 = Colors.Green;
                color2 = Colors.ForestGreen;
            }

            for (int i = 0; i < 500; i++)
            {
                double x1 = (i - 250) / 250.0;
                double y1 = myFunctions[j](x1);
                double x2 = (i - 249) / 250.0;
                double y2 = myFunctions[j](x2);
                this.DrawLine(
                    new Vector2((float)x1 * 300 + offset.X, size.Y - ((float)y1 * 20 + offset.Y)),
                    new Vector2((float)x2 * 300 + offset.X, size.Y - ((float)y2 * 20 + offset.Y)),
                    i % 2 == 0 ? color1 : color2,
                    1f,
                    true
                );
            }
        }
    }
}
