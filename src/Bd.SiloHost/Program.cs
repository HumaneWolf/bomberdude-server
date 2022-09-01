using Bd.Grains.Players;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

try
{
    var host = await StartSiloAsync();
    await host.WaitForShutdownAsync();

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    return 1;
}

static async Task<IHost> StartSiloAsync()
{
    var builder = new HostBuilder()
        .UseOrleans(c =>
        {
            c.UseLocalhostClustering(siloPort: 11111, gatewayPort: 30000)
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "BomberDude";
                })
                .ConfigureApplicationParts(
                    parts => parts.AddApplicationPart(typeof(Player).Assembly).WithReferences())
                .ConfigureLogging(logging => logging.AddConsole())
                .AddSimpleMessageStreamProvider("SMSProvider")
                .AddMemoryGrainStorage("PubSubStore");
        });

    var host = builder.Build();
    await host.StartAsync();

    return host;
}