using System.Collections.Generic;
using Godot;

public partial class OrbitLineDrawer : Control
{
    [Export]
    private Universe _universe;

    [Export]
    private UniverseView _universeView;

    [Export]
    private Camera3D _camera;

    private readonly Dictionary<string, MeshInstance3D> _orbitLineMeshesByCelestialBodyId = [];

    private readonly Dictionary<
        string,
        Dictionary<Patch, MeshInstance3D>
    > _patchLineMeshesByVesselId = [];

    private readonly Material _orbitLineMaterial = new StandardMaterial3D
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        AlbedoColor = Colors.Aqua,
    };

    private readonly Material _patchLineMaterial = new StandardMaterial3D
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        AlbedoColor = Colors.Green,
    };

    public override void _Ready()
    {
        _universe.UniverseInitialized += _GenerateOrbitLineMeshes;
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        _GeneratePatchLineMeshes();
        _UpdateOrbitLinePositions();
        _DrawClosestApproach();
        _DrawVelocities();
    }

    private void _GenerateOrbitLineMeshes()
    {
        foreach (var body in _universe.GetCelestialBodies())
        {
            if (body.Orbit == null)
            {
                continue;
            }

            var meshInstance = _GenerateEllipticalOrbitLineMesh(body.Orbit, _orbitLineMaterial);
            meshInstance.Name = $"{body.CelestialBodyId}_{meshInstance.GetType().Name}";
            _universeView.AddChild(meshInstance);

            _orbitLineMeshesByCelestialBodyId.Add(body.CelestialBodyId, meshInstance);
        }
    }

    private void _GeneratePatchLineMeshes()
    {
        foreach (var vessel in _universe.PopVesselsWithDirtyPatches())
        {
            HashSet<Patch> vesselPatches = new(vessel.Trajectory.Patches);

            Dictionary<Patch, MeshInstance3D> patchLineMeshesByPatch;
            if (
                !_patchLineMeshesByVesselId.TryGetValue(vessel.VesselId, out patchLineMeshesByPatch)
            )
            {
                patchLineMeshesByPatch = [];
                _patchLineMeshesByVesselId[vessel.VesselId] = patchLineMeshesByPatch;
            }

            foreach (var (patch, meshInstance) in patchLineMeshesByPatch)
            {
                if (!vesselPatches.Contains(patch))
                {
                    patchLineMeshesByPatch.Remove(patch);
                    meshInstance.QueueFree();
                }
            }

            foreach (var vesselPatch in vesselPatches)
            {
                if (!patchLineMeshesByPatch.ContainsKey(vesselPatch))
                {
                    MeshInstance3D meshInstance = _GeneratePatchLineMesh(vesselPatch);
                    meshInstance.Name =
                        $"{vessel.VesselId}_{meshInstance.GetType().Name}_{meshInstance.GetInstanceId()}";
                    _universeView.AddChild(meshInstance);

                    patchLineMeshesByPatch[vesselPatch] = meshInstance;
                }
            }

            _patchLineMeshesByVesselId[vessel.VesselId] = patchLineMeshesByPatch;
        }
    }

    private MeshInstance3D _GenerateEllipticalOrbitLineMesh(Orbit orbit, Material material)
    {
        var meshInstance = new MeshInstance3D();
        var immediateMesh = new ImmediateMesh();
        immediateMesh.ClearSurfaces();
        immediateMesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip, material);

        int resolution = 100;

        for (int i = 0; i <= resolution; i++)
        {
            double eccentricAnomaly = Mathf.DegToRad(i * 360f / resolution);
            var state = orbit.SolveStateAtEccentricAnomaly(eccentricAnomaly);
            var pos = (Vector3)state.Position;
            immediateMesh.SurfaceAddVertex(pos);
        }

        immediateMesh.SurfaceEnd();
        meshInstance.Mesh = immediateMesh;
        meshInstance.MaterialOverride = material;

        return meshInstance;
    }

    private MeshInstance3D _GeneratePatchLineMesh(Patch patch)
    {
        if (!patch.EndTime.HasValue)
        {
            return _GenerateEllipticalOrbitLineMesh(patch.Orbit, _patchLineMaterial);
        }

        var meshInstance = new MeshInstance3D();
        var immediateMesh = new ImmediateMesh();
        immediateMesh.ClearSurfaces();
        immediateMesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip, _patchLineMaterial);

        int resolution = 100;

        for (int i = 0; i <= resolution; i++)
        {
            var state = patch.Orbit.SolveStateAtTime(
                patch.StartTime.PlusSeconds(
                    ((double)i / resolution) * patch.StartTime.SecondsUntil(patch.EndTime.Value)
                )
            );
            var pos = (Vector3)state.Position;
            immediateMesh.SurfaceAddVertex(pos);
        }

        immediateMesh.SurfaceEnd();
        meshInstance.Mesh = immediateMesh;
        meshInstance.MaterialOverride = _patchLineMaterial;

        return meshInstance;
    }

    private void _UpdateOrbitLinePositions()
    {
        foreach (var (celestialBodyId, meshInstance) in _orbitLineMeshesByCelestialBodyId)
        {
            var body = _universe.GetCelestialBody(celestialBodyId);
            meshInstance.Position = (Vector3)
                OrbitUtils.CalculateAbsolutePosition(body.Orbit.CenterBody);
        }

        foreach (var (vesselId, meshInstances) in _patchLineMeshesByVesselId)
        {
            int i = 0;

            foreach (var (_, meshInstance) in meshInstances)
            {
                var vessel = _universe.GetVessel(vesselId);
                meshInstance.Position = (Vector3)
                    OrbitUtils.CalculateAbsolutePosition(
                        vessel.Trajectory.Patches[i].Orbit.CenterBody
                    );
                i++;
            }
        }
    }

    private void _DrawClosestApproach()
    {
        var body1 = _universe.GetCelestialBody("planet1");
        var body2 = _universe.GetCelestialBody("planet2");

        var timeOfClosestApproach = ApproachSolver.SolveClosestApproach(
            body1,
            body2,
            _universe.GetTime()
        );

        if (!timeOfClosestApproach.HasValue)
        {
            return;
        }

        var state1 = body1.Orbit.SolveStateAtTime(timeOfClosestApproach.Value);
        Vector3 pos1 = (Vector3)state1.Position;
        var state2 = body2.Orbit.SolveStateAtTime(timeOfClosestApproach.Value);
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
        foreach (var body in _universe.GetCelestialBodies())
        {
            if (body.Orbit == null)
            {
                continue;
            }

            var velocity = (Vector3)body.Velocity;
            var from = (Vector3)OrbitUtils.CalculateAbsolutePosition(body);
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
