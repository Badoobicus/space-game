using System.Collections.Generic;
using System.Collections.Immutable;

public class Trajectory(IEnumerable<Patch> patches)
{
    public ImmutableArray<Patch> Patches { get; } = patches.ToImmutableArray();
}
