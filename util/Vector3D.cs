using Godot;
using System;

public struct Vector3D : IEquatable<Vector3D>
{
    public static Vector3D Zero { get; } = new Vector3D(0, 0, 0);

    public double X;
    public double Y;
    public double Z;

    public Vector3D(double x, double y, double z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }

    public static Vector3D operator +(Vector3D left, Vector3D right)
    {
        left.X += right.X;
        left.Y += right.Y;
        left.Z += right.Z;
        return left;
    }

    public static Vector3D operator -(Vector3D left, Vector3D right)
    {
        left.X -= right.X;
        left.Y -= right.Y;
        left.Z -= right.Z;
        return left;
    }

    public static Vector3D operator *(Vector3D vec, double scale)
    {
        vec.X *= scale;
        vec.Y *= scale;
        vec.Z *= scale;
        return vec;
    }

    public static Vector3D operator *(double scale, Vector3D vec)
    {
        vec.X *= scale;
        vec.Y *= scale;
        vec.Z *= scale;
        return vec;
    }

    public static Vector3D operator /(Vector3D vec, double divisor)
    {
        vec.X /= divisor;
        vec.Y /= divisor;
        vec.Z /= divisor;
        return vec;
    }

    public static bool operator ==(Vector3D left, Vector3D right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector3D left, Vector3D right)
    {
        return !left.Equals(right);
    }

    public static implicit operator Vector3D(Vector3 vec)
    {
        return new Vector3D(vec.X, vec.Y, vec.Z);
    }

    public static explicit operator Vector3(Vector3D vec)
    {
        return new Vector3((float)vec.X, (float)vec.Y, (float)vec.Z);
    }

    public readonly double Length()
    {
        return Math.Sqrt(this.X * this.X + this.Y * this.Y + this.Z * this.Z);
    }

    public readonly double LengthSquared()
    {
        return this.X * this.X + this.Y * this.Y + this.Z * this.Z;
    }

    public readonly Vector3D Normalized()
    {
        double length = this.Length();
        if (length == 0)
        {
            return new Vector3D(0, 0, 0);
        }
        else
        {
            return new Vector3D(this.X / length, this.Y / length, this.Z / length);
        }
    }

    public readonly double DistanceTo(Vector3D other)
    {
        double x = other.X - this.X;
        double y = other.Y - this.Y;
        double z = other.Z - this.Z;
        return Math.Sqrt(x * x + y * y + z * z);
    }

    public readonly double DistanceSquaredTo(Vector3D other)
    {
        double x = other.X - this.X;
        double y = other.Y - this.Y;
        double z = other.Z - this.Z;
        return x * x + y * y + z * z;
    }

    public readonly double Dot(Vector3D other)
    {
        return this.X * other.X + this.Y * other.Y + this.Z * other.Z;
    }

    public readonly Vector3D Cross(Vector3D other)
    {
        return new Vector3D(
            (this.Y * other.Z) - (this.Z * other.Y),
            (this.Z * other.X) - (this.X * other.Z),
            (this.X * other.Y) - (this.Y * other.X)
        );
    }

    public readonly Vector3D Project(Vector3D onNormal)
    {
        return onNormal * (this.Dot(onNormal) / onNormal.LengthSquared());
    }

    public override readonly bool Equals(object obj)
    {
        return obj is Vector3D other && Equals(other);
    }

    public readonly bool Equals(Vector3D other)
    {
        return this.X == other.X && this.Y == other.Y && this.Z == other.Z;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(this.X, this.Y, this.Z);
    }

    public override readonly string ToString()
    {
        return $"({this.X}, {this.Y}, {this.Z})";
    }
}
