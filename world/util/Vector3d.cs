using System;
using Godot;

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

    public readonly double Length() => Math.Sqrt(X * X + Y * Y + Z * Z);

    public readonly double LengthSquared() => X * X + Y * Y + Z * Z;

    public readonly double Dot(Vector3d v) => X * v.X + Y * v.Y + Z * v.Z;

    public readonly Vector3d Cross(Vector3d v) =>
        new(Y * v.Z - Z * v.Y, Z * v.X - X * v.Z, X * v.Y - Y * v.X);

    public readonly double AngleTo(Vector3d to) => Math.Atan2(Cross(to).Length(), Dot(to));

    public static Vector3d operator +(Vector3d v1, Vector3d v2) =>
        new(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);

    public static Vector3d operator -(Vector3d v1, Vector3d v2) =>
        new(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);

    public static Vector3d operator *(Vector3d v, double s) => new(v.X * s, v.Y * s, v.Z * s);

    public static Vector3d operator *(double s, Vector3d v) => new(v.X * s, v.Y * s, v.Z * s);

    public static Vector3d operator /(Vector3d v, double s) => new(v.X / s, v.Y / s, v.Z / s);

    public static implicit operator Vector3d(Vector3 godotVec) =>
        new(godotVec.X, godotVec.Y, godotVec.Z);

    public static explicit operator Vector3(Vector3d customVec) =>
        new((float)customVec.X, (float)customVec.Y, (float)customVec.Z);

    public override readonly string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }
}
