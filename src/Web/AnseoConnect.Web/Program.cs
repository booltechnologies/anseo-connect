using AnseoConnect.Client;
using AnseoConnect.Web.Security;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddDevExpressBlazor();
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<SessionState>();
builder.Services.AddScoped<ClientTokenProvider>();
builder.Services.AddScoped<IClientTokenProvider>(sp => sp.GetRequiredService<ClientTokenProvider>());
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();

builder.Services.AddAnseoConnectApiClients(options =>
{
    builder.Configuration.GetSection("AnseoApi").Bind(options);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
