using Blazored.LocalStorage;
using BlazorFirebaseNotesApp.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace BlazorFirebaseNotesApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            // Add Blazored.LocalStorage
            builder.Services.AddBlazoredLocalStorage();

            // Add Auth services
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<FirebaseAuthStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<FirebaseAuthStateProvider>());
            //builder.Services.AddScoped<AuthService>();

            // Register the service that handles the raw sign-up/login API calls.
            // We use a standard named HttpClient for this, as it doesn't need the auth token.
            builder.Services.AddHttpClient<AuthService>();


            // Register our new message handler. It should be transient.
            builder.Services.AddTransient<FirebaseAuthenticationHandler>();

            // Get the Firebase Project ID from configuration
            var firebaseProjectId = builder.Configuration["Firebase:ProjectId"];

            // Configure a TYPED HttpClient for FirebaseService.
            // This is the modern, recommended approach.
            builder.Services.AddHttpClient<FirebaseService>(client =>
            {
                // Set the base address for all requests made by the FirebaseService
                client.BaseAddress = new Uri($"https://firestore.googleapis.com/");
            })
            // Add our custom handler to the pipeline for this specific HttpClient.
            // Every request made by FirebaseService will now go through our handler first.
            .AddHttpMessageHandler<FirebaseAuthenticationHandler>();


            // Add our Firebase data service
            //builder.Services.AddScoped<FirebaseService>();

            await builder.Build().RunAsync();
        }
    }
}
