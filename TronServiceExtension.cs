using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Tron.Exchange;

using Tron.Wallet.Net;

public record TronRecord(IServiceProvider ServiceProvider, ITronClient? TronClient, IOptions<TronNetOptions>? Options);

public static class TronServiceExtension {
    private static IServiceProvider AddTronNet() {
        IServiceCollection services = new ServiceCollection();
        services.AddTronNet(x => { x.Network = TronNetwork.MainNet; x.Channel = new GrpcChannelOption { Host = "grpc.trongrid.io", Port = 50051 }; x.SolidityChannel = new GrpcChannelOption { Host = "grpc.trongrid.io", Port = 50052 }; x.ApiKey = "bc7fee82-a7a3-449c-957f-7dd7e6475bf0"; });
        services.AddLogging();

        return services.BuildServiceProvider();
    }

    public static TronRecord GetRecord() {
        var provider = AddTronNet();
        var client = provider.GetService<ITronClient>();
        var options = provider.GetService<IOptions<TronNetOptions>>();

        return new TronRecord(provider, client, options);
    }
}