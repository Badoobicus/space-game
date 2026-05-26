public struct Vector3d
{
    public double X;
    public double Y;
    public double Z;

    public Vector3d(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vector3d operator +(Vector3d v1, Vector3d v2)
    {
        return new Vector3d(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
    }

    public static Vector3d operator -(Vector3d v1, Vector3d v2)
    {
        return new Vector3d(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
    }

    public static Vector3d operator *(Vector3d v, double s)
    {
        return new Vector3d(v.X * s, v.Y * s, v.Z * s);
    }

    public static Vector3d operator /(Vector3d v, double s)
    {
        return new Vector3d(v.X / s, v.Y / s, v.Z / s);
    }
}
