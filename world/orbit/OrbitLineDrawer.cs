using Godot;

public partial class OrbitLineDrawer : Control
{
    [Export]
    private World _world;

    [Export]
    private Camera3D _camera;

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        _DrawOrbits();
        _DrawClosestApproach();
        _DrawVelocities();
    }

    private void _DrawOrbits()
    {
        foreach (var body in _world.GetCelestialBodies())
        {
            if (body.Orbit == null)
            {
                continue;
            }

            var orbit = body.Orbit;
            var state = EllipticalOrbitSolver.SolveState(orbit, 0);
            var solver = OrbitSolver.FromInitialState(orbit.Body.Mass, state[0], state[1], 0);

            int resolution = 100;

            var material = new StandardMaterial3D();
            material.ShadingMode = StandardMaterial3D.ShadingModeEnum.Unshaded;
            material.AlbedoColor = new Color(0, 1, 1);

            for (int i = 0; i < resolution; i++)
            {
                double eccentricAnomaly = Mathf.DegToRad(i * 360f / resolution);
                var (finalPos, _) = solver.SolveStateAtEccentricAnomaly(eccentricAnomaly);
                var finalFinalPos = orbit.Body.Position + finalPos;
                if (!_camera.IsPositionBehind(finalFinalPos))
                {
                    Vector2 cameraPos = _camera.UnprojectPosition(finalFinalPos);
                    DrawCircle(cameraPos, 2, Colors.Aqua);
                }
            }
        }
    }

    private void _DrawClosestApproach()
    {
        var orbit1 = _world.GetCelestialBodies()[3].Orbit;
        var state1 = EllipticalOrbitSolver.SolveState(orbit1, 0);
        var solver1 = OrbitSolver.FromInitialState(orbit1.Body.Mass, state1[0], state1[1], 0);

        var orbit2 = _world.GetCelestialBodies()[1].Orbit;
        var state2 = EllipticalOrbitSolver.SolveState(orbit2, 0);
        var solver2 = OrbitSolver.FromInitialState(orbit2.Body.Mass, state2[0], state2[1], 0);

        var timeOfClosestApproach = ApproachSolver.SolveClosestApproach(
            orbit1,
            orbit2,
            _world.GetTime()
        );

        if (timeOfClosestApproach < 0)
        {
            return;
        }

        var (pos1, _) = solver1.SolveStateAtTime(timeOfClosestApproach);
        var (pos2, _) = solver2.SolveStateAtTime(timeOfClosestApproach);

        if (!_camera.IsPositionBehind(pos1))
        {
            DrawCircle(_camera.UnprojectPosition(pos1), 5, Colors.Green);
        }

        if (!_camera.IsPositionBehind(pos2))
        {
            DrawCircle(_camera.UnprojectPosition(pos2), 5, Colors.Green);
        }
    }

    private void _DrawVelocities()
    {
        foreach (var body in _world.GetCelestialBodies())
        {
            if (body.Orbit == null)
            {
                continue;
            }

            var orbit = body.Orbit;
            var state = EllipticalOrbitSolver.SolveState(orbit, 0);
            var solver = OrbitSolver.FromInitialState(orbit.Body.Mass, state[0], state[1], 0);
            var (_, velocity) = solver.SolveStateAtTime(_world.GetTime());
            var from = body.Position;
            var to = from + velocity;

            if (!_camera.IsPositionBehind(to) && !_camera.IsPositionBehind(from))
            {
                DrawLine(
                    _camera.UnprojectPosition(from),
                    _camera.UnprojectPosition(to),
                    Colors.Red
                );
            }
        }
    }
}
