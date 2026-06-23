public struct StateVector(Vector3d position, Vector3d velocity)
{
    public Vector3d Position = position;
    public Vector3d Velocity = velocity;

    public override readonly string ToString()
    {
        return $"[Pos: {Position}, Vel: {Velocity}]";
    }
}
