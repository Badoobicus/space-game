using Godot;
using System;

public class Timer
{
    private string name;
    private long start;

    private Timer(string name)
    {
        this.name = name;
        this.start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }

    public Timer Stop()
    {
        long end = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        GD.Print($"{name}: {end - start}ms");
        return this;
    }

    public static Timer Start(string name)
    {
        return new Timer(name);
    }
}
