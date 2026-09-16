using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Platform.Web;
using Platform.Web.Authentication;
using Platform.Web.Foundation;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl is required.");
builder.Services.AddScoped<PlatformApiAuthorizationMessageHandler>();
builder.Services.AddScoped(sp =>
{
    var authorizationHandler = sp.GetRequiredService<PlatformApiAuthorizationMessageHandler>();
    authorizationHandler.InnerHandler = new HttpClientHandler();
    return new HttpClient(authorizationHandler) { BaseAddress = new Uri(apiBaseUrl) };
});
builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Oidc", options.ProviderOptions);
    options.UserOptions.RoleClaim = "role";
});
builder.Services.AddScoped<ExperienceContext>();
builder.Services.AddScoped<GovernedExperienceContextClient>();

await builder.Build().RunAsync();
