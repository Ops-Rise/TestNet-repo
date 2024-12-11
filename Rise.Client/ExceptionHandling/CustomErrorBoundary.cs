using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Rise.Client.ExceptionHandling
{
    public class CustomErrorBoundary : ErrorBoundary
    {
        [Inject]
        private IWebAssemblyHostEnvironment env { get; set; }

        protected override Task OnErrorAsync(Exception exception)
        {
            if (env.IsDevelopment())
            {   
                //in development log in console of the error
                return base.OnErrorAsync(exception);
            }
            //in production no log in console of the error
            return Task.CompletedTask;
        }
    }
}
