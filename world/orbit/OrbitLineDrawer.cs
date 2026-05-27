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
            var initialState = EllipticalOrbitSolver.SolveState(orbit, 0);
            var solver = OrbitSolver.FromInitialState(initialState, orbit.Body.Mass, 0);

            int resolution = 100;

            var material = new StandardMaterial3D();
            material.ShadingMode = StandardMaterial3D.ShadingModeEnum.Unshaded;
            material.AlbedoColor = new Color(0, 1, 1);

            for (int i = 0; i < resolution; i++)
            {
                double eccentricAnomaly = Mathf.DegToRad(i * 360f / resolution);
                var state = solver.SolveStateAtEccentricAnomaly(eccentricAnomaly);
                var pos = (Vector3)(orbit.Body.Position + state.Position);
                if (!_camera.IsPositionBehind(pos))
                {
                    Vector2 cameraPos = _camera.UnprojectPosition(pos);
                    DrawCircle(cameraPos, 2, Colors.Aqua);
                }
            }
        }
    }

    private void _DrawClosestApproach()
    {
        var orbit1 = _world.GetCelestialBodies()[3].Orbit;
        var initialState1 = EllipticalOrbitSolver.SolveState(orbit1, 0);
        var solver1 = OrbitSolver.FromInitialState(initialState1, orbit1.Body.Mass, 0);

        var orbit2 = _world.GetCelestialBodies()[1].Orbit;
        var initialState2 = EllipticalOrbitSolver.SolveState(orbit2, 0);
        var solver2 = OrbitSolver.FromInitialState(initialState2, orbit2.Body.Mass, 0);

        var timeOfClosestApproach = ApproachSolver.SolveClosestApproach(
            orbit1,
            orbit2,
            _world.GetTime()
        );

        if (timeOfClosestApproach < 0)
        {
            return;
        }

        var state1 = solver1.SolveStateAtTime(timeOfClosestApproach);
        Vector3 pos1 = (Vector3)state1.Position;
        var state2 = solver2.SolveStateAtTime(timeOfClosestApproach);
        Vector3 pos2 = (Vector3)state2.Position;

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
            var initialState = EllipticalOrbitSolver.SolveState(orbit, 0);
            var solver = OrbitSolver.FromInitialState(initialState, orbit.Body.Mass, 0);
            var state = solver.SolveStateAtTime(_world.GetTime());
            var velocity = (Vector3)state.Velocity;
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
