namespace VMPerPath;
using System.Net.Http;

using Yarp.ReverseProxy.Forwarder;
class RequestTransformer : HttpTransformer
{
    public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix)
    {
        await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix).ConfigureAwait(false);
        var pathSegments = httpContext.Request.Path.Value!.Split('/'); 
        var newPath = "/";
        
        if (pathSegments.Length > 2)
        {
            newPath = $"/{string.Join("/", pathSegments.Skip(2)).TrimEnd('/')}";
        }

        proxyRequest.RequestUri = RequestUtilities.MakeDestinationAddress(destinationPrefix, newPath, httpContext.Request.QueryString);
    }
}