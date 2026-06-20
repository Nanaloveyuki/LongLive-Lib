using System.Collections.Generic;

namespace LongLive.Mods.Compatibility;

public sealed class LongLiveCompatibilitySnapshot
{
    public IReadOnlyCollection<LongLiveCompatibilityLibraryDescriptor> Libraries { get; set; } = new LongLiveCompatibilityLibraryDescriptor[0];

    public IReadOnlyCollection<LongLiveCompatibilityRedirectDescriptor> Redirects { get; set; } = new LongLiveCompatibilityRedirectDescriptor[0];

    public IReadOnlyCollection<LongLiveCompatibilityActivationRecord> Activations { get; set; } = new LongLiveCompatibilityActivationRecord[0];
}
