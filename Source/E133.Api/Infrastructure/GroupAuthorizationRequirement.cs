using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

namespace E133.Api.Infrastructure
{
    internal class GroupAuthorizationRequirement : AuthorizationHandler<GroupAuthorizationRequirement>, IAuthorizationRequirement
    {
        private readonly string _group;

        public GroupAuthorizationRequirement(string group)
        {
            this._group = group;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, GroupAuthorizationRequirement requirement)
        {
            var mvcContext = context.Resource as Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext;
            if (mvcContext != null)
            {
                if (mvcContext.HttpContext.Request.Headers.ContainsKey("x-admin"))
                {
                    if (bool.Parse(mvcContext.HttpContext.Request.Headers["x-admin"]))
                    {
                        context.Succeed(requirement);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
