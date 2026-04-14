using Godot;
using System;

public partial class OrbitLineDrawerTest : Control
{
    [Export]
    private Camera3D camera;

    [Export]
    private World world;

    public override void _Draw()
    {
        foreach (var celestialBody in world.CelestialBodies)
        {
            EllipticalOrbit orbit = new EllipticalOrbit(
                celestialBody.Orbit.Current.OrbitalParameters
            );
            if (orbit != null)
            {
                int segments = 100;
                Vector3 lastWorldPosition =
                    (Vector3)orbit.GetPositionAtEccentricAnomaly(0)
                    + orbit.CelestialBody.GlobalPosition;
                Vector2 lastViewportPosition = camera.UnprojectPosition(lastWorldPosition);

                for (int i = 1; i <= segments; i++)
                {
                    Vector3 worldPosition =
                        (Vector3)
                            orbit.GetPositionAtEccentricAnomaly(2 * Math.PI * (i / (float)segments))
                        + orbit.CelestialBody.GlobalPosition;
                    Vector2 viewportPosition = camera.UnprojectPosition(worldPosition);

                    if (
                        !this.camera.IsPositionBehind(lastWorldPosition)
                        && !this.camera.IsPositionBehind(worldPosition)
                    )
                    {
                        this.DrawLine(
                            lastViewportPosition,
                            viewportPosition,
                            i % 2 == 0 ? Colors.Blue : Colors.Cyan,
                            0.5f,
                            true
                        );
                    }

                    lastWorldPosition = worldPosition;
                    lastViewportPosition = viewportPosition;
                }
            }
        }

        if (true)
        {
            ChebychevTest.Approximate(
                new EllipticalOrbit(world.CelestialBodies[1].Orbit.Current.OrbitalParameters),
                new EllipticalOrbit(world.CelestialBodies[3].Orbit.Current.OrbitalParameters),
                world.ElapsedTime
            );
        }

        if (false)
        {
            double start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            try
            {
                GD.Print(
                    CalculateTimeToIntercept(
                        new EllipticalOrbit(
                            world.CelestialBodies[1].Orbit.Current.OrbitalParameters
                        ),
                        new EllipticalOrbit(
                            world.CelestialBodies[3].Orbit.Current.OrbitalParameters
                        ),
                        world.ElapsedTime
                    )
                );
            }
            catch (MathNet.Numerics.NonConvergenceException ex)
            {
                GD.Print("does not converge");
            }
            double end = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            GD.Print("ms: " + (end - start));
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        this.QueueRedraw();
    }

    public static double? CalculateTimeToIntercept(
        EllipticalOrbit majorObject,
        EllipticalOrbit minorObject,
        double initialTime
    )
    {
        double minorOrbitalPeriod = GetOrbitalPeriod(minorObject);

        // Define function for distance at a given time
        Func<double, double> distanceFunction = (time) =>
            (majorObject.GetPositionAtTime(time) - minorObject.GetPositionAtTime(time)).Length()
            - 3;

        // TODO approximate roots using Chebychev polynomial instead of scanning the whole orbit in one go.
        // The Brent root finder works best when the start and end values have different signs, so we'll want to narrow down the search first,
        // then use the root finder to get a more exact answer.

        // Try https://physics.stackexchange.com/questions/90774/determinstic-implementation-sphere-of-influence-change-using-patched-conic-appro
        // or https://space.stackexchange.com/questions/23127/how-can-i-find-the-time-of-flight-between-spheres-of-influence

        // Find roots of the distance function (potential intercepts)
        double root = MathNet.Numerics.RootFinding.Brent.FindRoot(
            distanceFunction,
            initialTime,
            initialTime + 1
        );

        return root;
    }

    private static double GetOrbitalPeriod(EllipticalOrbit parameters)
    {
        // Calculate orbital period based on Kepler's laws
        double gravitationalConstant = 1; // Replace with actual gravitational constant value
        double semiMajorAxis = parameters.SemiMajorAxis;
        return 2
            * Math.PI
            * Math.Sqrt(
                Math.Pow(semiMajorAxis, 3) / (gravitationalConstant * parameters.CelestialBody.Mass)
            );
    }
}
