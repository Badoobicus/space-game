using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class OrbitLineDrawer : Control
{
    [Export]
    private Camera3D camera;

    [Export]
    private World world;

    [Export]
    private TextEdit xVelocityInput;

    [Export]
    private TextEdit yVelocityInput;

    [Export]
    private TextEdit zVelocityInput;

    [Export]
    private Control test;
    private Vector3D startingPosition;
    private Vector3D startingVelocity;

    public override void _Ready()
    {
        this.world.WorldReady += () =>
        {
            this.startingPosition = this.world.Vessels[1].Orbit.GetPositionAtTime(0);
            this.startingVelocity = this.world.Vessels[1].Orbit.GetVelocityAtTime(0);
            this.xVelocityInput.Text = this.startingVelocity.X.ToString();
            this.yVelocityInput.Text = this.startingVelocity.Y.ToString();
            this.zVelocityInput.Text = this.startingVelocity.Z.ToString();
        };
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3D velocity = this.startingVelocity;
        double value;

        if (double.TryParse(this.xVelocityInput.Text, out value))
        {
            velocity.X = value;
        }

        if (double.TryParse(this.yVelocityInput.Text, out value))
        {
            velocity.Y = value;
        }

        if (double.TryParse(this.zVelocityInput.Text, out value))
        {
            velocity.Z = value;
        }

        this.world.Vessels[1].Orbit = EllipticalOrbit.FromPositionAndVelocity(
            this.startingPosition,
            velocity,
            this.world.Vessels[1].Orbit.CelestialBody
        );
    }

    public override void _Draw()
    {
        // this.DrawOrbitLines();
        this.DrawVesselOrbitLines();
        this.DrawClosestApproaches();
        this.DrawRelativeVelocityOfOrbits();
        this.DrawHyperbolicOrbit();
    }

    private void DrawOrbitLines()
    {
        int segments = 100;

        List<Task<Vector2[]>> tasks = new List<Task<Vector2[]>>();

        foreach (var celestialBody in world.CelestialBodies)
        {
            Orbit orbit = celestialBody.Orbit;
            if (orbit != null && orbit.Current is EllipticalOrbitSegment ellipticalOrbitSegment)
            {
                tasks.Add(
                    Task.Run(() =>
                    {
                        Vector2[] viewportPositions = new Vector2[segments + 1];

                        for (int i = 0; i < viewportPositions.Length; i++)
                        {
                            OrbitalState state = ellipticalOrbitSegment.GetStateAtEccentricAnomaly(
                                (i / (double)segments) * (Math.PI * 2)
                            );
                            Vector3 worldPosition =
                                state.CentralBody.Position + (Vector3)state.Position;
                            viewportPositions[i] = this.camera.IsPositionBehind(worldPosition)
                                ? -Vector2.One
                                : this.camera.UnprojectPosition(worldPosition);
                        }

                        return viewportPositions;
                    })
                );

                {
                    OrbitalState state = ellipticalOrbitSegment.GetStateAtTime(world.ElapsedTime);
                    Vector3 position =
                        (Vector3)state.Position
                        + ellipticalOrbitSegment.OrbitalParameters.CentralBody.GlobalPosition;
                    Vector3 velocity = (Vector3)state.Velocity;
                    Vector2 cameraPosition1 = camera.UnprojectPosition(position);
                    Vector2 cameraPosition2 = camera.UnprojectPosition(position + velocity);
                    if (
                        !camera.IsPositionBehind(position)
                        && !camera.IsPositionBehind(position + velocity)
                    )
                    {
                        this.DrawLine(cameraPosition1, cameraPosition2, Colors.Red, 0.5f, true);
                    }
                }
            }
        }

        foreach (var task in tasks)
        {
            task.Wait();
            if (task.IsCompletedSuccessfully)
            {
                Vector2[] viewportPositions = task.Result;

                for (int i = 0; i < viewportPositions.Length - 1; i++)
                {
                    Vector2 viewportPosition1 = viewportPositions[i];
                    Vector2 viewportPosition2 = viewportPositions[i + 1];
                    if (viewportPosition1.X >= 0 && viewportPosition2.X >= 0)
                    {
                        // TODO refactor this to create a mesh using PrimitiveType.PRIMITIVE_LINES and have the GPU handle it
                        // instead of calling DrawLine() for each segment
                        this.DrawLine(
                            viewportPosition1,
                            viewportPosition2,
                            i % 2 == 0 ? Colors.Blue : Colors.Cyan,
                            0.5f,
                            true
                        );
                    }
                }
            }
        }
    }

    // Draw orbit lines by calculating world position at various eccentric anomalies
    private void DrawVesselOrbitLines()
    {
        foreach (var orbit in world.Vessels.Select(vessel => vessel.Orbit))
        {
            if (orbit != null)
            {
                int segments = 100;
                Vector2[] viewportPositions = new Vector2[segments + 1];

                Vector3 lastWorldPosition =
                    (Vector3)orbit.GetPositionAtEccentricAnomaly(0)
                    + orbit.CelestialBody.GlobalPosition;
                Vector2 lastViewportPosition = camera.UnprojectPosition(lastWorldPosition);
                viewportPositions[0] = lastViewportPosition;

                for (int i = 1; i <= segments; i++)
                {
                    Vector3 worldPosition =
                        (Vector3)
                            orbit.GetPositionAtEccentricAnomaly(2 * Math.PI * (i / (float)segments))
                        + orbit.CelestialBody.GlobalPosition;
                    Vector2 viewportPosition = camera.UnprojectPosition(worldPosition);
                    viewportPositions[i] = viewportPosition;

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

                Vector3 position =
                    (Vector3)orbit.GetPositionAtTime(world.ElapsedTime)
                    + orbit.CelestialBody.GlobalPosition;
                Vector3 velocity = (Vector3)orbit.GetVelocityAtTime(world.ElapsedTime);
                Vector2 cameraPosition1 = camera.UnprojectPosition(position);
                Vector2 cameraPosition2 = camera.UnprojectPosition(position + velocity);
                if (
                    !camera.IsPositionBehind(position)
                    && !camera.IsPositionBehind(position + velocity)
                )
                {
                    this.DrawLine(cameraPosition1, cameraPosition2, Colors.Red, 0.5f, true);
                }
            }
        }
    }

    private void DrawClosestApproaches()
    {
        VesselOld vessel = this.world.Vessels[0];
        CelestialBody body = this.world.CelestialBodies[1];

        long start = System.DateTimeOffset.UtcNow.Millisecond;
        List<double> approachTimes = vessel.CalculateTimeOfClosestApproaches(world, body);

        int i = 0;
        foreach (double time in approachTimes)
        {
            Color color;
            if (i == 0)
            {
                color = Colors.MintCream;
            }
            else if (i == 1)
            {
                color = Colors.Green;
            }
            else if (i == 2)
            {
                color = Colors.ForestGreen;
            }
            else
            {
                color = Colors.DarkGreen;
            }

            Vector3 position =
                (Vector3)vessel.Orbit.GetPositionAtTime(time)
                + vessel.Orbit.CelestialBody.GlobalPosition;
            Vector2 cameraPosition = camera.UnprojectPosition(position);
            this.DrawCircle(cameraPosition, 5, color);

            OrbitalState bodyState = body.Orbit.GetStateAtTime(time);
            Vector3 bodyPosition = bodyState.CentralBody.Position + (Vector3)bodyState.Position;
            Vector2 bodyCameraPosition = camera.UnprojectPosition(bodyPosition);
            this.DrawCircle(bodyCameraPosition, 5, color);

            i++;
        }
    }

    private void DrawRelativeVelocityOfOrbits()
    {
        EllipticalOrbit orbit = this.world.Vessels[0].Orbit;
        if (orbit != null)
        {
            CelestialBody celestialBody = this.world.CelestialBodies[1];

            double period = orbit.Period;
            int segments = 16;
            Vector3 lastWorldPosition =
                (Vector3)orbit.GetPositionAtTime(world.ElapsedTime)
                + orbit.CelestialBody.GlobalPosition;
            Vector2 lastViewportPosition = camera.UnprojectPosition(lastWorldPosition);

            for (int i = 1; i <= segments; i++)
            {
                double time = world.ElapsedTime + period * (i / (float)segments);
                Vector3 worldPosition =
                    (Vector3)orbit.GetPositionAtTime(time) + orbit.CelestialBody.GlobalPosition;
                Vector2 viewportPosition = camera.UnprojectPosition(worldPosition);

                if (
                    !this.camera.IsPositionBehind(lastWorldPosition)
                    && !this.camera.IsPositionBehind(worldPosition)
                )
                {
                    // this.DrawLine(
                    //     lastViewportPosition,
                    //     viewportPosition,
                    //     vessel.CalculateRelativeDistanceVelocityAtTime(celestialBody, time) < 0
                    //         ? Colors.Blue
                    //         : Colors.Red,
                    //     0.5f,
                    //     true
                    // );
                }

                lastWorldPosition = worldPosition;
                lastViewportPosition = viewportPosition;
            }
        }
    }

    private void DrawHyperbolicOrbit()
    {
        List<HyperbolicOrbit> orbits = new List<HyperbolicOrbit>();

        orbits.Add(
            new HyperbolicOrbit
            {
                CelestialBody = this.world.CelestialBodies[0],
                SemiMajorAxis = 18,
                Eccentricity = 1.1,
                Inclination = 0,
                ArgumentOfPeriapsis = 0,
                LongitudeOfAscendingNode = 0,
            }
        );

        orbits.Add(
            new HyperbolicOrbit
            {
                CelestialBody = this.world.CelestialBodies[0],
                SemiMajorAxis = 18,
                Eccentricity = 1.5,
                Inclination = 0,
                ArgumentOfPeriapsis = 0,
                LongitudeOfAscendingNode = 0,
            }
        );

        orbits.Add(
            new HyperbolicOrbit
            {
                CelestialBody = this.world.CelestialBodies[0],
                SemiMajorAxis = 18,
                Eccentricity = 2,
                Inclination = 1,
                ArgumentOfPeriapsis = 0,
                LongitudeOfAscendingNode = 0,
            }
        );

        orbits.Add(
            HyperbolicOrbit.FromPositionAndVelocity(
                new Vector3D(0, 0, -20),
                new Vector3D(Math.Sqrt(2 * this.world.CelestialBodies[0].Mass / 20) + 1e-8, 0, 0),
                this.world.CelestialBodies[0]
            )
        );

        // GD.Print(
        //     HyperbolicOrbit
        //         .FromPositionAndVelocity(
        //             new Vector3D(0, 0, -20),
        //             new Vector3D(-15, 0, 0),
        //             this.world.CelestialBodies[0]
        //         )
        //         .GetPositionAtTime(100_000_000_000_000)
        // );

        foreach (HyperbolicOrbit orbit in orbits)
        {
            for (int i = -50; i < 50; i++)
            {
                Vector3 worldPosition1 =
                    (Vector3)orbit.GetPositionAtTime(Math.Sinh(i / 10.0))
                    + orbit.CelestialBody.GlobalPosition;
                Vector2 viewportPosition1 = camera.UnprojectPosition(worldPosition1);

                Vector3 worldPosition2 =
                    (Vector3)orbit.GetPositionAtTime(Math.Sinh((i + 1) / 10.0))
                    + orbit.CelestialBody.GlobalPosition;
                Vector2 viewportPosition2 = camera.UnprojectPosition(worldPosition2);

                if (
                    !this.camera.IsPositionBehind(worldPosition1)
                    && !this.camera.IsPositionBehind(worldPosition2)
                )
                {
                    this.DrawLine(
                        viewportPosition1,
                        viewportPosition2,
                        i % 2 == 0 ? Colors.Blue : Colors.Cyan,
                        0.5f,
                        true
                    );
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        this.QueueRedraw();
    }
}
