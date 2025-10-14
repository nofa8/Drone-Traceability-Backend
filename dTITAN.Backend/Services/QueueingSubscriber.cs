namespace dTITAN.Backend.Services;

public class QueueingSubscriber
{
    private readonly DroneMessageQueue _queue;

    public QueueingSubscriber(DroneMessageQueue queue)
    {
        _queue = queue;
    }

    public Task HandleMessageAsync(string msg)
    {
        // Fire-and-wait on channel write; if channel is full this will await (backpressure)
        return _queue.EnqueueAsync(msg).AsTask();
    }
}
