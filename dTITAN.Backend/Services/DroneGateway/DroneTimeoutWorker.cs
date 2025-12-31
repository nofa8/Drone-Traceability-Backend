namespace dTITAN.Backend.Services.DroneGateway;

public class DroneTimeoutWorker(DroneManager manager, TimeSpan timeout) : BackgroundService
{
    private readonly DroneManager _manager = manager;
    private readonly TimeSpan _timeout = timeout;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            _manager.SweepDisconnected();
            await Task.Delay(_timeout, ct);
        }
    }
}
