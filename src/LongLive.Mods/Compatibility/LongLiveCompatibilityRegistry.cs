using System;
using System.Collections.Generic;

namespace LongLive.Mods.Compatibility;

public sealed class LongLiveCompatibilityRegistry : ILongLiveCompatibilityRegistry
{
    private readonly Dictionary<string, LongLiveCompatibilityLibraryDescriptor> _libraries = new Dictionary<string, LongLiveCompatibilityLibraryDescriptor>(StringComparer.Ordinal);
    private readonly Dictionary<string, LongLiveCompatibilityRedirectDescriptor> _redirects = new Dictionary<string, LongLiveCompatibilityRedirectDescriptor>(StringComparer.Ordinal);
    private readonly Dictionary<string, LongLiveCompatibilityActivationRecord> _activations = new Dictionary<string, LongLiveCompatibilityActivationRecord>(StringComparer.Ordinal);

    public void RegisterLibrary(LongLiveCompatibilityLibraryDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.LibraryId))
        {
            throw new ArgumentException("Compatibility library descriptor must define a library ID.", nameof(descriptor));
        }

        _libraries[descriptor.LibraryId] = descriptor;
    }

    public void RegisterRedirect(LongLiveCompatibilityRedirectDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.RedirectId))
        {
            throw new ArgumentException("Compatibility redirect descriptor must define a redirect ID.", nameof(descriptor));
        }

        _redirects[descriptor.RedirectId] = descriptor;
    }

    public void RecordActivation(LongLiveCompatibilityActivationRecord record)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        if (string.IsNullOrWhiteSpace(record.RedirectId))
        {
            throw new ArgumentException("Compatibility activation record must define a redirect ID.", nameof(record));
        }

        var existing = GetActivationOrCreate(record.RedirectId);
        existing.SourceLibraryId = record.SourceLibraryId;
        existing.SourceDetected = record.SourceDetected;
        existing.RedirectEnabled = record.RedirectEnabled;
        existing.RedirectApplied = record.RedirectApplied;
        existing.StatusCode = record.StatusCode;
        existing.Detail = record.Detail;
    }

    public LongLiveCompatibilityActivationRecord GetActivationOrCreate(string redirectId)
    {
        if (string.IsNullOrWhiteSpace(redirectId))
        {
            throw new ArgumentException("Compatibility activation record must define a redirect ID.", nameof(redirectId));
        }

        if (_activations.TryGetValue(redirectId, out var record))
        {
            return record;
        }

        record = new LongLiveCompatibilityActivationRecord
        {
            RedirectId = redirectId,
        };

        _activations[redirectId] = record;
        return record;
    }

    public LongLiveCompatibilitySnapshot CaptureSnapshot()
    {
        return new LongLiveCompatibilitySnapshot
        {
            Libraries = new List<LongLiveCompatibilityLibraryDescriptor>(_libraries.Values).AsReadOnly(),
            Redirects = new List<LongLiveCompatibilityRedirectDescriptor>(_redirects.Values).AsReadOnly(),
            Activations = new List<LongLiveCompatibilityActivationRecord>(_activations.Values).AsReadOnly(),
        };
    }
}
