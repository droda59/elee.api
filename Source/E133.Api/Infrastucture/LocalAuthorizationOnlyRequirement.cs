using Microsoft.AspNetCore.Authorization;

namespace E133.Api.Infrastructure
{
    public class LocalAuthorizationOnlyRequirement : AuthorizationHandler<LocalAuthorizationOnlyRequirement>, IAuthorizationRequirement
    {
        protected override void Handle(AuthorizationContext context, LocalAuthorizationOnlyRequirement requirement)
        {
            var mvcContext = context.Resource as Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext;
            if (mvcContext != null)
            {
                if (mvcContext.HttpContext.Request.Host.Host == "localhost")
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
            }
        }
    }
}