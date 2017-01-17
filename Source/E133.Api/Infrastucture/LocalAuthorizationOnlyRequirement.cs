using Microsoft.AspNetCore.Authorization;

using System.Threading.Tasks;

namespace E133.Api.Infrastructure
{
    public class LocalAuthorizationOnlyRequirement : AuthorizationHandler<LocalAuthorizationOnlyRequirement>, IAuthorizationRequirement
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LocalAuthorizationOnlyRequirement requirement)
        {
            var mvcContext = context.Resource as Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext;
            if (mvcContext != null)
            {
                if (mvcContext.HttpContext.Request.Host.Host == "localhost")
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}