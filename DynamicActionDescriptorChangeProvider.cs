using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Primitives;
using System.Threading;

namespace dotnet_roslyn_dynamic_api
{
    // Enables signaling invalidation of the cached collection of ActionDescriptors
    public class DynamicActionDescriptorChangeProvider : IActionDescriptorChangeProvider
    {
        public static DynamicActionDescriptorChangeProvider Instance { get; } = new DynamicActionDescriptorChangeProvider();

        public CancellationTokenSource TokenSource { get; private set; }

        public bool HasChanged { get; set; }

        public IChangeToken GetChangeToken()
        {
            TokenSource = new CancellationTokenSource();
            return new CancellationChangeToken(TokenSource.Token);
        }
    }
}