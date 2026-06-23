using System;
using System.Collections.Generic;
using System.Collections.Immutable;

public class Trajectory
{
    private readonly ImmutableArray<Patch> _patches;

    public ImmutableArray<Patch> Patches => _patches;
    public Patch CurrentPatch => _patches[0];

    public Trajectory(ICollection<Patch> patches)
    {
        if (patches == null || patches.Count == 0)
        {
            throw new ArgumentException("Patches cannot be null or empty.");
        }

        _patches = [.. patches];
    }

    public Trajectory FilterActivePatches(double time)
    {
        if (time < CurrentPatch.EndTime)
        {
            return this;
        }

        List<Patch> patches = [];

        foreach (var patch in Patches)
        {
            if (patch.EndTime > time)
            {
                patches.Add(patch);
            }
        }

        if (patches.Count > 0)
        {
            patches[0] = new Patch(patches[0].Orbit, patches[0].StartTime, patches[0].EndTime);
        }

        return new Trajectory(patches);
    }
}
