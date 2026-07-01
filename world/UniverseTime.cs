public struct UniverseTime
{
    public const long SecondsPerMajorUnit = 10_000_000;
    public static UniverseTime Zero = new(0, 0);

    public readonly long MajorUnits;
    public readonly double Seconds;
    public double TotalSeconds => MajorUnits * SecondsPerMajorUnit + Seconds;

    public UniverseTime(long majorUnits, double seconds)
    {
        long totalMajorUnits = majorUnits + (long)(seconds / SecondsPerMajorUnit);
        double remainingSeconds = seconds % SecondsPerMajorUnit;

        if (totalMajorUnits > 0 && remainingSeconds < 0)
        {
            totalMajorUnits--;
            remainingSeconds += SecondsPerMajorUnit;
        }
        else if (totalMajorUnits < 0 && remainingSeconds > 0)
        {
            totalMajorUnits++;
            remainingSeconds -= SecondsPerMajorUnit;
        }

        MajorUnits = totalMajorUnits;
        Seconds = remainingSeconds;
    }

    public readonly double SecondsUntil(UniverseTime t)
    {
        return (t.MajorUnits - MajorUnits) * SecondsPerMajorUnit + t.Seconds - Seconds;
    }

    public readonly UniverseTime PlusSeconds(double seconds)
    {
        return new UniverseTime(MajorUnits, Seconds + seconds);
    }

    public override bool Equals(object obj)
    {
        return obj is UniverseTime t && t.MajorUnits == MajorUnits && t.Seconds == Seconds;
    }

    public override string ToString()
    {
        return $"{MajorUnits}:{Seconds}";
    }

    public static UniverseTime FromSeconds(double seconds)
    {
        return new UniverseTime(0, seconds);
    }

    public static UniverseTime operator +(UniverseTime t1, UniverseTime t2) =>
        new(t1.MajorUnits + t2.MajorUnits, t1.Seconds + t2.Seconds);

    public static UniverseTime operator -(UniverseTime t1, UniverseTime t2) =>
        new(t1.MajorUnits - t2.MajorUnits, t1.Seconds - t2.Seconds);

    public static bool operator <(UniverseTime t1, UniverseTime t2)
    {
        return t1.MajorUnits < t2.MajorUnits
            || (t1.MajorUnits == t2.MajorUnits && t1.Seconds < t2.Seconds);
    }

    public static bool operator <=(UniverseTime t1, UniverseTime t2)
    {
        return t1.MajorUnits < t2.MajorUnits
            || (t1.MajorUnits == t2.MajorUnits && t1.Seconds <= t2.Seconds);
    }

    public static bool operator >(UniverseTime t1, UniverseTime t2)
    {
        return t1.MajorUnits > t2.MajorUnits
            || (t1.MajorUnits == t2.MajorUnits && t1.Seconds > t2.Seconds);
    }

    public static bool operator >=(UniverseTime t1, UniverseTime t2)
    {
        return t1.MajorUnits > t2.MajorUnits
            || (t1.MajorUnits == t2.MajorUnits && t1.Seconds >= t2.Seconds);
    }
}
