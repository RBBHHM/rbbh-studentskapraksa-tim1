using RBBH.RelatedPartiesRegister.Web.Components;
using RBBH.RelatedPartiesRegister.Web.Api;
using RBI.Hosting;
using RBI.Authentication;
using RBI.Authorization;
using RBI.Localization;
using RBBH.RelatedPartiesRegister.Web.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRbiHosting();
builder.Services.AddScoped<AppText>();
builder.Services.AddRbiLocalization(options => options.DefaultCulture = "bs");
// Local-development fallback only. Production must register the approved
// shared IDistributedCache provider before AddRbiAuthentication.
var identityConfigured = IdentityConfiguration.Resolve(builder.Configuration, builder.Environment);
builder.Services.AddSingleton(identityConfigured);
if (identityConfigured.Enabled && builder.Environment.IsDevelopment())
{
    builder.Services.AddDistributedMemoryCache();
}
if (identityConfigured.Enabled)
    builder.Services.AddRbiAuthentication(builder.Configuration);
else
    builder.Services.AddAuthentication(); // No handler, fabricated user or sign-in endpoint.
builder.Services.AddApplicationAuthorization();
builder.Services.AddAppApi(builder.Configuration, builder.Environment, identityConfigured);

var app = builder.Build();

app.UseRbiHosting();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRbiLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRbiLocalizationEndpoints();
if (identityConfigured.Enabled) app.MapRbiAuthenticationEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
