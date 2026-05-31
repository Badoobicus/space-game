using System.Collections.Generic;
using Godot;

public partial class OrbitLineDrawer : Control
{
    [Export]
    private World _world;

    [Export]
    private Camera3D _camera;

    private Dictionary<string, MeshInstance3D> _orbitLineMeshesByCelestialBodyId = new();

    public override void _Ready()
    {
        _world.CelestialBodiesInitialized += _GenerateOrbitLineMeshes;
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        _UpdateOrbitLinePositions();
        _DrawClosestApproach();
        _DrawVelocities();
    }

    private void _GenerateOrbitLineMeshes()
    {
        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = Colors.Aqua,
        };

        foreach (var body in _world.GetCelestialBodies())
        {
            if (body.Orbit == null)
            {
                continue;
            }

            var meshInstance = new MeshInstance3D();
            var immediateMesh = new ImmediateMesh();
            immediateMesh.ClearSurfaces();
            immediateMesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip, material);

            int resolution = 100;

            for (int i = 0; i <= resolution; i++)
            {
                double eccentricAnomaly = Mathf.DegToRad(i * 360f / resolution);
                var state = body.OrbitSolver.SolveStateAtEccentricAnomaly(eccentricAnomaly);
                var pos = (Vector3)(body.Orbit.Body.Position + state.Position);
                immediateMesh.SurfaceAddVertex(pos);
            }

            immediateMesh.SurfaceEnd();
            meshInstance.Mesh = immediateMesh;
            meshInstance.MaterialOverride = material;
            _world.AddChild(meshInstance);

            _orbitLineMeshesByCelestialBodyId.Add(body.CelestialBodyId, meshInstance);
        }
    }

    private void _UpdateOrbitLinePositions()
    {
        foreach (var (celestialBodyId, meshInstance) in _orbitLineMeshesByCelestialBodyId)
        {
            meshInstance.Position = (Vector3)
                _world.GetCelestialBody(celestialBodyId).Orbit.Body.Position;
        }
    }

    private void _DrawClosestApproach()
    {
        var body1 = _world.GetCelestialBody("planet1");
        var body2 = _world.GetCelestialBody("planet2");

        var timeOfClosestApproach = ApproachSolver.SolveClosestApproach(
            body1,
            body2,
            _world.GetTime()
        );

        if (timeOfClosestApproach < 0)
        {
            return;
        }

        var state1 = body1.OrbitSolver.SolveStateAtTime(timeOfClosestApproach);
        Vector3 pos1 = (Vector3)state1.Position;
        var state2 = body2.OrbitSolver.SolveStateAtTime(timeOfClosestApproach);
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

            var velocity = (Vector3)body.Velocity;
            var from = (Vector3)body.Position;
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
