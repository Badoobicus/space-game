using Godot;

public partial class Part : RigidBody3D
{
    [Export]
    private Node3D radialAttachmentPoint;

    public Vector3 GetRadialAttachmentOffset()
    {
        return radialAttachmentPoint.Position;
    }
}
