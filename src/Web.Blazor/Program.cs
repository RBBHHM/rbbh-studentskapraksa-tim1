using RBBH.RelatedPartiesRegister.Web.Components;
using RBBH.RelatedPartiesRegister.Web.Api;
using RBI.Hosting;
using RBI.Authentication;
using RBI.Authorization;
using RBI.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRbiHosting();
builder.Services.AddScoped<AppText>();
builder.Services.AddRbiLocalization();
// Local-development fallback only. Production must register the approved
// shared IDistributedCache provider before AddRbiAuthentication.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDistributedMemoryCache();
}
builder.Services.AddRbiAuthentication(builder.Configuration);
builder.Services.AddApplicationAuthorization();
var apiBaseUrl = builder.Services.AddAppApi(builder.Configuration, builder.Environment);

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
app.MapRbiAuthenticationEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
