namespace dTITAN.Backend.Services.Ingestion;

public class DroneTimeoutWorker(DroneManager manager) : BackgroundService
{
    private readonly DroneManager _manager = manager;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            _manager.SweepDisconnected();
            await Task.Delay(_interval, ct);
        }
    }
}
