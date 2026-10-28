namespace RBBH.RelatedPartiesRegister.Web.Security;

public sealed record IdentityConfiguration(bool Enabled)
{
    public static IdentityConfiguration Resolve(IConfiguration configuration, IHostEnvironment environment)
    {
        var values = new[] { "Authority", "ClientId", "ClientSecret" }
            .Select(key => configuration[$"Authentication:{key}"]).ToArray();
        if (values.All(string.IsNullOrWhiteSpace))
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("Configure application authentication outside Development.");
            return new(false);
        }
        // Partial settings fail in platform validation, never silently downgrade.
        return new(true);
    }
}
