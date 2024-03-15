using BeSwarm.CoreWebApi.Services.Errors;
using BeSwarm.CoreWebApi.Services.Tokens;

using System.Data;
using System.Diagnostics;

namespace BeSwarm.CoreWebApi.MiddleWare;

public class CatchExceptionMiddleWare
{
    private RequestDelegate next;
    ILogger logger;

    IDispatchError dispatch_error;
    public CatchExceptionMiddleWare(IDispatchError _dispatch_error, ITokenService _tokenservice, RequestDelegate next, ILogger<CatchExceptionMiddleWare> _logger)
    {
        this.next = next;
        logger = _logger;

        dispatch_error = _dispatch_error;
    }
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next.Invoke(context);
        }
        catch (Exception e)
        {
            await dispatch_error.DispatchCritical(e, "Uncatched exception", $"http request:{context.Request.Path.Value}");
        }

    }
}
public static class CatchExceptionMiddleWareHandler
{
    public static IApplicationBuilder UseCatchExceptionMiddleWareHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CatchExceptionMiddleWare>();
    }
}
