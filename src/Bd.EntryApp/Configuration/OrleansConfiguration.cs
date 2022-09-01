using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Bd.EntryApp.Configuration;

public static class OrleansConfiguration
{
    public static async Task<IClusterClient> AddOrleans(this IServiceCollection services)
    {
        var client = new ClientBuilder()
            .UseLocalhostClustering(gatewayPort: 30000)
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "dev";
                options.ServiceId = "BomberDude";
            })
            .ConfigureLogging(logging => logging.AddConsole())
            .AddSimpleMessageStreamProvider("SMSProvider")
            .Build();

        await client.Connect();
        
        services.AddSingleton<IClusterClient>(client);
        
        return client;
    }
}