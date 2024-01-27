using Godot;

public static class QuaternionExtension
{
    public static Quaternion LookRotation(this Quaternion quaternion, Vector3 forward, Vector3 up)
    {
        return Transform3D.Identity.LookingAt(forward, up).Basis.GetRotationQuaternion();
    }
}
