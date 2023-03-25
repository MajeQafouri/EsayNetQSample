using EasyNetQ;
using EasyNetQ.DI;
using QueueTest;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hosted,services) =>
    {
        var configuration = hosted.Configuration;
        services.AddHostedService<SubscriberWorker>();
        services.AddHostedService<PublisherWorker>();

        services.AddSingleton<BusServcie>();
        services.AddSingleton<DapperContext>();

        services.AddSingleton(RabbitHutch.CreateBus(
                                configuration["EasyNetQConfig:ConnectionString"],
                                serviceRegister =>
                                {
                                    serviceRegister.Register<ITypeNameSerializer, CustomTypeNameSerializer>();

                                }));

        

    })
    .Build();

await host.RunAsync();

