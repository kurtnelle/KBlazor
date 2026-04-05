namespace KBlazor.Services;

/// <summary>
/// Provides license validation for KBlazor.
/// Saving views (FlexTable) and saving edits (BasicEdit) require a valid license.
/// </summary>
public interface ILicenseProvider
{
    /// <summary>True if a valid, machine-locked license is present.</summary>
    bool IsLicensed { get; }

    /// <summary>Human-readable status message (e.g. "Licensed to Acme Corp" or "Unlicensed — saving disabled").</summary>
    string StatusMessage { get; }

    /// <summary>The machine fingerprint for this host (used in key signing requests).</summary>
    string MachineFingerprint { get; }
}
