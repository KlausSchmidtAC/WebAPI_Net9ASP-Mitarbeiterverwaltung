namespace Domain.Constants;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;


[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public class RequiresClaimAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _claimName;
    public readonly string _claimValue;
    public RequiresClaimAttribute(string claimName, string claimValue)
    {
        _claimName = claimName;
        _claimValue = claimValue;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        var hasClaim = user.HasClaim(_claimName,_claimValue);
        if (!hasClaim)
        {
            context.Result = new ForbidResult(); 
            return;
        }

        await Task.CompletedTask;
    }

}
