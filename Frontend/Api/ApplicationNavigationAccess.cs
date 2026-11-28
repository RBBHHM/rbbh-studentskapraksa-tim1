namespace RBBH.RelatedPartiesRegister.Web.Api;

/// <summary>Shared navigation projection from one effective application profile.</summary>
public sealed record ApplicationNavigationAccess(
    bool Administration,
    bool PeriodLock,
    bool Reports,
    bool Physical,
    bool Legal,
    bool Limits,
    bool Capital)
{
    public static async Task<ApplicationNavigationAccess> ReadAsync(
        IProfileApi profile, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var effective = await profile.GetAsync(cancellationToken);
        return new(
            ApplicationAccess.CanViewAdministration(effective),
            ApplicationAccess.CanViewPeriodLock(effective),
            ApplicationAccess.HasEffectiveApplicationAccess(effective),
            effective.Permissions.Contains("FL_VIEW"),
            effective.Permissions.Contains("PL_VIEW"),
            effective.Permissions.Contains("LIMITI_VIEW"),
            effective.Permissions.Contains("KAPITAL_VIEW"));
    }
}
