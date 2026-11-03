using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PlantCare.Api.Security;

[AttributeUsage(AttributeTargets.Method)]
public sealed class StreamingPhotoUploadAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        for (var i = context.ValueProviderFactories.Count - 1; i >= 0; i--)
            if (context.ValueProviderFactories[i] is FormValueProviderFactory or FormFileValueProviderFactory or JQueryFormValueProviderFactory)
                context.ValueProviderFactories.RemoveAt(i);
    }
    public void OnResourceExecuted(ResourceExecutedContext context) { }
}

public sealed class PhotoUploadAdmission
{
    private readonly SemaphoreSlim gate = new(1, 1);
    public IDisposable? TryEnter() => gate.Wait(0) ? new Lease(gate) : null;
    private sealed class Lease(SemaphoreSlim gate) : IDisposable
    {
        private SemaphoreSlim? held = gate;
        public void Dispose() => Interlocked.Exchange(ref held, null)?.Release();
    }
}
