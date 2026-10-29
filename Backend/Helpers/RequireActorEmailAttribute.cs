using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RBBH.ConnectedParties.Helpers;

[AttributeUsage(AttributeTargets.Class)]
public sealed class RequireActorEmailAttribute : ActionFilterAttribute
{
    public RequireActorEmailAttribute() => Order = -3000;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        try { _ = AuthenticatedActor.Email(context.HttpContext.User); }
        catch (MissingActorIdentityException)
        {
            context.Result = new UnauthorizedObjectResult(new ProblemDetails
            {
                Status = 401,
                Title = "Identitet nije potpun.",
                Detail = "Prijavite se ponovo. Autentificirani email je obavezan."
            });
        }
    }
}
