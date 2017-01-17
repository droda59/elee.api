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
                bool isAdmin = false;
                if (bool.TryParse(mvcContext.HttpContext.Request.Headers["X-Admin"], out isAdmin))
                {
                    if (isAdmin)
                    {
                        context.Succeed(requirement);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
