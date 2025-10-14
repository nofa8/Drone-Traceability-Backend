using System.Threading.Channels;

namespace dTITAN.Backend.Services;

public class DroneMessageQueue
{
    private readonly Channel<string> _channel;

    public DroneMessageQueue(int capacity = 5000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<string>(options);
    }

    public ValueTask EnqueueAsync(string message, CancellationToken ct = default) => _channel.Writer.WriteAsync(message, ct);

    public IAsyncEnumerable<string> ReadAllAsync(CancellationToken ct = default) => _channel.Reader.ReadAllAsync(ct);
}
