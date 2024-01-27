using Godot;
using Godot.Collections;

public partial class Build : Node3D
{
    private const float ZOOM_FACTOR = 1.15f;

    [Export]
    private Node3D cameraAnchor = null;

    [Export]
    private Camera3D camera = null;

    [Export]
    private Button fuelTankButton = null;

    [Export]
    private PackedScene fuelTankPrefab = null;

    [Export]
    private Button engineButton = null;

    [Export]
    private PackedScene enginePrefab = null;

    [Export]
    private Button symmetryButton = null;

    private float baseCameraDistance = 0;
    private Vector2 cameraRotation = Vector2.Zero;
    private float cameraDistance = 10;
    private Vector2 mousePosition = Vector2.Zero;
    private Vector2 lastMousePosition = Vector2.Zero;

    private Array<Node3D> vehicles = new Array<Node3D>();
    private Part grabbedPart = null;
    private bool symmetryEnabled = false;

    private bool shouldSpawnPart = false;

    public override void _Ready()
    {
        this.grabbedPart = null;

        this.cameraRotation = new Vector2(
            this.cameraAnchor.Rotation.Y,
            this.cameraAnchor.Rotation.X
        );

        this.fuelTankButton.Pressed += () =>
        {
            this.SpawnPart(this.fuelTankPrefab);
        };

        this.enginePrefab.Instantiate();
        this.engineButton.Pressed += () =>
        {
            this.SpawnPart(this.enginePrefab);
        };

        // TODO duplicate symmetry label code
        this.symmetryButton.Text = $"Symmetry: [{(this.symmetryEnabled ? "ON" : "OFF")}]";
        symmetryButton.Pressed += () =>
        {
            // TODO implement symmetry mode
            this.symmetryEnabled = !this.symmetryEnabled;
            this.symmetryButton.Text = $"Symmetry: [{(this.symmetryEnabled ? "ON" : "OFF")}]";
        };
    }

    public override void _Process(double delta)
    {
        this.mousePosition = this.GetViewport().GetMousePosition();

        this.HandleRotateCamera((float)delta);
        this.HandleMoveCamera((float)delta);

        var hoverResult = this.RaycastHover();

        this.HandleMoveGrabbedPart(hoverResult);
        this.HandleClick(hoverResult);

        this.lastMousePosition = this.mousePosition;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        this.HandleCameraZoom();
    }

    private void HandleRotateCamera(float delta)
    {
        if (Input.IsActionPressed("rotate_camera"))
        {
            this.cameraRotation -= (this.mousePosition - this.lastMousePosition) * delta;
            this.cameraRotation = new Vector2(
                this.cameraRotation.X,
                Mathf.Clamp(this.cameraRotation.Y, Mathf.DegToRad(-90), Mathf.DegToRad(90))
            );
            this.cameraAnchor.Rotation = new Vector3(
                this.cameraRotation.Y,
                this.cameraRotation.X,
                0
            );
        }
    }

    private void HandleMoveCamera(float delta)
    {
        if (Input.IsActionPressed("move_camera"))
        {
            Vector3 mousePositionInWorld = this.camera.ProjectPosition(
                this.mousePosition,
                this.cameraDistance
            );
            Vector3 lastMousePositionInWorld = this.camera.ProjectPosition(
                this.lastMousePosition,
                this.cameraDistance
            );
            this.cameraAnchor.Position -= mousePositionInWorld - lastMousePositionInWorld;
        }
    }

    private void HandleCameraZoom()
    {
        bool zoomIn = Input.IsActionJustPressed("zoom_in");
        bool zoomOut = Input.IsActionJustPressed("zoom_out");

        if (zoomIn)
        {
            this.cameraDistance /= ZOOM_FACTOR;
        }
        else if (zoomOut)
        {
            this.cameraDistance *= ZOOM_FACTOR;
        }

        if (zoomIn || zoomOut)
        {
            this.camera.Position = new Vector3(0, 0, this.cameraDistance);
        }
    }

    private void SpawnPart(PackedScene part)
    {
        if (this.grabbedPart != null)
        {
            this.RemoveChild(this.grabbedPart);
        }

        this.grabbedPart = (Part)part.Instantiate();
        this.AddChild(this.grabbedPart);
    }

    private Dictionary RaycastHover()
    {
        var query = new PhysicsRayQueryParameters3D();
        query.From = this.camera.GlobalPosition;
        query.To = this.camera.ProjectPosition(this.mousePosition, 100);
        if (this.grabbedPart != null)
        {
            Array<Rid> exclude = new Array<Rid>();
            exclude.Add(this.grabbedPart.GetRid());
            foreach (Node node in this.grabbedPart.GetAllChildren())
            {
                if (node is Part)
                {
                    exclude.Add(((Part)node).GetRid());
                }
            }
            query.Exclude = exclude;
        }
        return this.GetWorld3D().DirectSpaceState.IntersectRay(query);
    }

    private void HandleMoveGrabbedPart(Dictionary hoverResult)
    {
        if (this.grabbedPart != null)
        {
            var hoveredPart =
                hoverResult.Count > 0 && (Node)hoverResult["collider"] is Part
                    ? (Part)hoverResult["collider"]
                    : null;

            if (hoveredPart != null)
            {
                var hitPosition = (Vector3)hoverResult["position"];
                var hitNormal = (Vector3)hoverResult["normal"];

                this.grabbedPart.Quaternion = Quaternion.LookRotation(
                    -hitNormal,
                    hitNormal.Dot(hoveredPart.Basis.Y) < 0.99
                        ? hoveredPart.Basis.Y
                        : hoveredPart.Basis.Z
                );
                this.grabbedPart.GlobalPosition =
                    hitPosition - this.grabbedPart.Basis * hoveredPart.GetRadialAttachmentOffset();
            }
            else
            {
                this.grabbedPart.GlobalPosition = this.camera.ProjectPosition(
                    this.mousePosition,
                    this.cameraDistance
                );
                this.grabbedPart.Quaternion = Quaternion.Identity;
            }
        }
    }

    private void HandleClick(Dictionary hoverResult)
    {
        if (Input.IsActionJustPressed("select_part"))
        {
            var hoveredPart =
                hoverResult.Count > 0 && (Node)hoverResult["collider"] is Part
                    ? (Part)hoverResult["collider"]
                    : null;

            if (this.grabbedPart != null)
            {
                Node3D parent = hoveredPart != null ? hoveredPart : this.CreateVehicle();
                this.grabbedPart.Reparent(parent);
                this.grabbedPart = null;
            }
            else if (hoveredPart != null)
            {
                this.grabbedPart = hoveredPart;
                this.grabbedPart.Reparent(this);
                this.RemoveEmptyVehicles();
            }
        }
    }

    private Node3D CreateVehicle()
    {
        Node3D vehicle = new Node3D();
        this.AddChild(vehicle);
        this.vehicles.Add(vehicle);
        return vehicle;
    }

    private void RemoveEmptyVehicles()
    {
        for (int i = 0; i < vehicles.Count; i++)
        {
            Node3D vehicle = this.vehicles[i];
            if (vehicle.GetChildCount() == 0)
            {
                this.RemoveChild(vehicle);
                this.vehicles.RemoveAt(i);
                i--;
            }
        }
    }
}
