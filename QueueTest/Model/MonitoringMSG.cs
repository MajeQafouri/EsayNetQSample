using EasyNetQ;

namespace QueueTest.Model;

[Queue("TestQueueName", ExchangeName = "TestExchage")]
public class MonitoringMSG
{

    public DateTime TimeStamp { get; set; }
}