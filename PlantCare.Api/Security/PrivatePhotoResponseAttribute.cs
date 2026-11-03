using Microsoft.AspNetCore.Mvc.Filters;

namespace PlantCare.Api.Security;

public sealed class PrivatePhotoResponseAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        context.HttpContext.Items[typeof(PrivatePhotoResponseAttribute)] = true;
    }
}
