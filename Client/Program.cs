using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;
using NDAProcesses.Shared.Models;
using NDAProcesses.Client;
using NDAProcesses.Client.Services;
using NDAProcesses.Shared.Services;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddRadzenComponents();
builder.Services.AddHttpClient();
builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });


builder.Services.AddScoped<IUserService, UserServiceProxy>();
builder.Services.AddScoped<IMessageService, MessageServiceProxy>();

builder.Services.AddScoped<CookieHelper>();

var host = builder.Build();
await host.RunAsync();
