using EasyNetQ;

public class BusServcie 
{
    private readonly IBus bus;

    public BusServcie(IBus bus)
    {
        this.bus = bus;
    }
    public async Task PublishAsync<TMessage>(TMessage message) where TMessage : class
    {
        await bus.PubSub.PublishAsync(message);
    }

    public async Task SubscribeToTopicAsync<TMessage>(Func<TMessage, Task> processor) where TMessage : class
    {
          await bus.PubSub.SubscribeAsync(typeof(TMessage).Name, processor);
    }
     
}
