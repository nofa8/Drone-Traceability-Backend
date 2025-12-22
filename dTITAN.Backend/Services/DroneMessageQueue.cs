using System.Threading.Channels;
using dTITAN.Backend.DTO;

namespace dTITAN.Backend.Services;

/// <summary>
/// Lightweight, bounded asynchronous queue for incoming drone messages.
/// Uses a <see cref="System.Threading.Channels.Channel{T}"/> to provide backpressure
/// and efficient producer/consumer semantics for the rest of the application.
/// </summary>
public class DroneMessageQueue
{
    private readonly Channel<DroneEnvelope> _channel;
    private readonly ILogger<DroneMessageQueue> _logger;

    /// <summary>
    /// Creates a new <see cref="DroneMessageQueue"/> with the provided logger
    /// and an optional bounded capacity.
    /// </summary>
    /// <param name="logger">Logger used for diagnostics.</param>
    /// <param name="capacity">Maximum number of items the queue will buffer before applying backpressure.</param>
    public DroneMessageQueue(ILogger<DroneMessageQueue> logger, int capacity = 5000)
    {
        _logger = logger;
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<DroneEnvelope>(options);
    }

    /// <summary>
    /// Enqueues a <see cref="DroneEnvelope"/> into the channel asynchronously.
    /// When the channel is full this method will await, providing backpressure to callers.
    /// </summary>
    /// <param name="message">The message envelope to enqueue.</param>
    /// <param name="ct">Cancellation token to cancel the enqueue operation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous enqueue operation.</returns>
    public ValueTask EnqueueAsync(DroneEnvelope message, CancellationToken ct = default)
    {
        _logger.LogDebug("Enqueueing message for {DroneId}", message.DroneId);
        return _channel.Writer.WriteAsync(message, ct);
    }

    /// <summary>
    /// Reads all available <see cref="DroneEnvelope"/> instances from the queue
    /// as an asynchronous stream. Consumers should iterate this to process messages.
    /// </summary>
    /// <param name="ct">Cancellation token to stop streaming.</param>
    /// <returns>An async-enumerable of <see cref="DroneEnvelope"/> messages.</returns>
    public IAsyncEnumerable<DroneEnvelope> ReadAllAsync(CancellationToken ct = default) =>
        _channel.Reader.ReadAllAsync(ct);
}

