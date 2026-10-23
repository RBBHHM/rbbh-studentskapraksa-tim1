using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.Middlewares;

namespace UnitTests.Middleware;

// Route/method matrix for EVERY mutating controller action. A new mutating action that is
// not classified here fails the build of this suite, so period-lock coverage can never
// silently depend on a path-prefix allowlist again.
public sealed class PeriodLockCoverageTests
{
    private const bool Locked = true;
    private const bool Open = false;

    // METHOD route-template -> (blocked while the period is locked?, rationale)
    private static readonly Dictionary<string, (bool Locked, string Rationale)> Matrix = new(StringComparer.OrdinalIgnoreCase)
    {
        // Reportable business data -> LOCKED
        ["POST api/related-persons"] = (Locked, "creates physical related person"),
        ["PUT api/related-persons/{id}"] = (Locked, "edits physical related person"),
        ["DELETE api/related-persons/{id}"] = (Locked, "removes physical related person"),
        ["POST api/related-persons/{id}/family-members"] = (Locked, "changes family relationships"),
        ["PUT api/related-persons/{id}/family-members/{familyMemberId}"] = (Locked, "changes family relationships"),
        ["DELETE api/related-persons/{id}/family-members/{familyMemberId}"] = (Locked, "changes family relationships"),
        ["POST api/related-persons/import"] = (Locked, "bulk creates physical persons"),
        ["POST api/legal-entities"] = (Locked, "creates legal person"),
        ["PUT api/legal-entities/{id}"] = (Locked, "edits legal person"),
        ["DELETE api/legal-entities/{id}"] = (Locked, "removes legal person"),
        ["POST api/legal-entities/import"] = (Locked, "bulk creates legal persons"),
        ["POST api/limiti"] = (Locked, "creates limit"),
        ["PUT api/limiti/{id}"] = (Locked, "edits limit"),
        ["DELETE api/limiti/{id}"] = (Locked, "removes limit"),
        ["PUT api/capital"] = (Locked, "changes regulatory/core/supplementary capital that reports are computed from"),
        ["POST api/verification/physical-person/{id}"] = (Locked, "changes verification state of reportable person"),
        ["POST api/verification/legal-person/{id}"] = (Locked, "changes verification state of reportable legal person"),

        // Must stay usable while locked -> OPEN
        ["POST api/period-lock/lock"] = (Open, "period administration"),
        ["POST api/period-lock/unlock"] = (Open, "period administration: unlock must work while locked"),
        ["POST api/period-lock/request-unlock"] = (Open, "unlock workflow"),
        ["POST api/period-lock/unlock-requests/{id}/reject"] = (Open, "unlock workflow"),
        ["POST api/period-lock/unlock-requests/{id}/request-info"] = (Open, "unlock workflow"),
        ["POST api/period-lock/unlock-requests/{id}/respond"] = (Open, "unlock workflow"),
        ["POST api/reports/daily"] = (Open, "persists a snapshot but does not change period data (business decision)"),
        ["POST api/reports/monthly/{year}/{month}"] = (Open, "persists a snapshot but does not change period data (business decision)"),
        ["POST api/users"] = (Open, "user administration"),
        ["POST api/users/{userId}/roles"] = (Open, "user administration"),
        ["PUT api/users/{userId}/role"] = (Open, "user administration"),
        ["DELETE api/users/{userId}"] = (Open, "user administration"),
        ["POST api/users/{userId}/reactivate"] = (Open, "user administration"),
        ["DELETE api/users/{userId}/permanent"] = (Open, "user administration"),
        ["POST api/code-lists/categories"] = (Open, "configuration data, not period data"),
        ["DELETE api/code-lists/categories/{name}"] = (Open, "configuration data, not period data"),
        ["POST api/code-lists"] = (Open, "configuration data, not period data"),
        ["POST api/code-lists/{kategorija}"] = (Open, "configuration data, not period data"),
        ["PUT api/code-lists/{id}"] = (Open, "configuration data, not period data"),
        ["DELETE api/code-lists/{id}"] = (Open, "configuration data, not period data"),
        ["POST api/code-lists/{kategorija}/import"] = (Open, "configuration data, not period data"),
    };

    private static readonly string[] MutatingVerbs = ["POST", "PUT", "DELETE", "PATCH"];

    private static IEnumerable<(string Key, string Method, string Path)> MutatingActions()
    {
        foreach (var controller in typeof(PeriodLockMiddleware).Assembly.GetTypes()
                     .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract))
        {
            var classRoute = controller.GetCustomAttributes<RouteAttribute>().FirstOrDefault()?.Template;
            foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var routeFromMethod = action.GetCustomAttributes<RouteAttribute>().FirstOrDefault()?.Template;
                foreach (var http in action.GetCustomAttributes<HttpMethodAttribute>())
                foreach (var method in http.HttpMethods.Where(verb => MutatingVerbs.Contains(verb)))
                {
                    var template = string.Join('/', new[] { classRoute, routeFromMethod, http.Template }
                        .Where(part => !string.IsNullOrEmpty(part)));
                    var normalized = Regex.Replace(template, @"\{(\w+):\w+\}", "{$1}");
                    var path = "/" + Regex.Replace(normalized, @"\{(\w+)\}", match => match.Groups[1].Value switch
                    {
                        "id" or "familyMemberId" or "reportId" => Guid.NewGuid().ToString(),
                        "year" => "2026",
                        "month" => "3",
                        _ => "sample"
                    });
                    // Normalize the id placeholders the same way the matrix does.
                    yield return ($"{method} {Regex.Replace(normalized, @"\{(?:familyMemberId)\}", "{familyMemberId}")}", method, path);
                }
            }
        }
    }

    [Fact]
    public void EveryMutatingControllerActionIsClassifiedInTheMatrix()
    {
        var actual = MutatingActions().Select(action => action.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.NotEmpty(actual);
        Assert.Empty(actual.Except(Matrix.Keys));      // new mutating route: decide lock or open explicitly
        Assert.Empty(Matrix.Keys.Except(actual));      // stale matrix row
    }

    [Fact]
    public async Task MiddlewareBlocksExactlyTheLockedRowsWhileThePeriodIsLocked()
    {
        var mismatches = new List<string>();
        foreach (var action in MutatingActions())
        {
            var (blocked, called) = await Run(action.Method, action.Path, periodLocked: true);
            if (blocked != Matrix[action.Key].Locked || called == blocked)
                mismatches.Add($"{action.Key}: expected locked={Matrix[action.Key].Locked}, middleware blocked={blocked}");
        }
        Assert.Empty(mismatches);
    }

    [Fact]
    public async Task NothingIsBlockedWhileThePeriodIsOpen()
    {
        foreach (var action in MutatingActions())
        {
            var (blocked, called) = await Run(action.Method, action.Path, periodLocked: false);
            Assert.False(blocked, action.Key);
            Assert.True(called, action.Key);
        }
    }

    [Theory]
    [InlineData("GET", "/api/capital")]
    [InlineData("GET", "/api/capital/export")]
    [InlineData("GET", "/api/related-persons")]
    [InlineData("GET", "/api/reports/daily")]
    [InlineData("GET", "/api/reports/export/all-clients")]
    [InlineData("GET", "/api/audit-logs")]
    [InlineData("GET", "/api/auth/profile")]
    public async Task ReadOnlyRequestsAreNeverBlocked(string method, string path)
    {
        var (blocked, called) = await Run(method, path, periodLocked: true);
        Assert.False(blocked);
        Assert.True(called);
    }

    [Theory]
    [InlineData("PUT", "/API/CAPITAL")]
    [InlineData("POST", "/Api/Verification/physical-person/abc")]
    public async Task LockCheckIsCaseInsensitive(string method, string path)
    {
        var (blocked, _) = await Run(method, path, periodLocked: true);
        Assert.True(blocked);
    }

    private static async Task<(bool Blocked, bool NextCalled)> Run(string method, string path, bool periodLocked)
    {
        var repository = Substitute.For<IPeriodLockRepository>();
        repository.IsCurrentPeriodLockedAsync(Arg.Any<string?>()).Returns(periodLocked);
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        var called = false;
        var middleware = new PeriodLockMiddleware(_ => { called = true; return Task.CompletedTask; }, NullLogger<PeriodLockMiddleware>.Instance);

        await middleware.InvokeAsync(context, repository);

        return (context.Response.StatusCode == StatusCodes.Status423Locked, called);
    }
}
