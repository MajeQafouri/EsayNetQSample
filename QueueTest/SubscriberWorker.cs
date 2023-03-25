using Dapper;
using EasyNetQ;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QueueTest.Model;
using System.Collections.Concurrent; 

namespace QueueTest
{
    public class SubscriberWorker : BackgroundService
    {
        private readonly ILogger<SubscriberWorker> _logger;
        private readonly BusServcie busServcie;
        private readonly DapperContext context;

        public SubscriberWorker(ILogger<SubscriberWorker> logger, BusServcie busServcie, DapperContext context)
        {
            _logger = logger;
            this.busServcie = busServcie;
            this.context = context;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await busServcie.SubscribeToTopicAsync<MonitoringMSG>(ProcessMessage); 
        }

        private async Task ProcessMessage(MonitoringMSG message)
        {
            try
            { 
                var query = @$"INSERT INTO [dbo].[Messages] ([Message]) VALUES ('{JsonConvert.SerializeObject(message)}')";

                using (var connection = context.CreateConnection())
                {
                    await connection.ExecuteAsync(query);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, JsonConvert.SerializeObject(message));
            }
        }
    }

    public class PublisherWorker : BackgroundService
    {
        private readonly ILogger<PublisherWorker> _logger;
        private readonly BusServcie busServcie;

        public PublisherWorker(ILogger<PublisherWorker> logger, BusServcie busServcie)
        {
            _logger = logger;
            this.busServcie = busServcie;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await busServcie.PublishAsync(new MonitoringMSG { TimeStamp = DateTime.Now });

                _logger.LogInformation("Message Published");

                await Task.Delay(100);
            }
        }
    }

    //This serializer will be used to serialize and deserialized all message through EasyNetQ
    //you need to put all your message in the same folder with same  namespace
    //in this project all messages' namepace will be started by "QueueTest.Model"
    public class CustomTypeNameSerializer : ITypeNameSerializer
    {
        private readonly ConcurrentDictionary<string, Type> deserializedTypes = new ConcurrentDictionary<string, Type>();

        public Type DeSerialize(string typeName)
        {
            return deserializedTypes.GetOrAdd(typeName, t =>
            {
                var type = Type.GetType($"QueueTest.Model.{t}" + ", " + "QueueTest");
                if (type == null)
                    throw new EasyNetQException("Cannot find type {0}", t);
                return type;
            });
        }

        private readonly ConcurrentDictionary<Type, string> serializedTypes = new ConcurrentDictionary<Type, string>();

        public string Serialize(Type type)
        {

            return serializedTypes.GetOrAdd(type, t =>
            {

                var typeName = t.Name;
                if (typeName.Length > 255)
                    throw new EasyNetQException("The serialized name of type '{0}' exceeds the AMQP " +
                                                "maximum short string length of 255 characters.", t.Name);
                return typeName;
            });
        }
    }

}
