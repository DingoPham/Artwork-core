using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace ArtworkCore.FilterAttribute
{
    public class CustomFilter : ActionFilterAttribute
    {
        public CustomFilter()
        {
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            IPAddress remoteIp = context.HttpContext.Connection.RemoteIpAddress;
            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
        }
    }
}
