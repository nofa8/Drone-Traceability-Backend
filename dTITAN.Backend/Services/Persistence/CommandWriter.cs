using MongoDB.Driver;
using dTITAN.Backend.Data.Models.Events;
using dTITAN.Backend.Data.Persistence;
using dTITAN.Backend.Services.EventBus;

namespace dTITAN.Backend.Services.Persistence;

public class CommandWriter
{
    private readonly IMongoCollection<DroneCommandDocument> _commands;
    private readonly ILogger<CommandWriter> _logger;

    public CommandWriter(IMongoCollection<DroneCommandDocument> commands, IEventBus eventBus, ILogger<CommandWriter> logger)
    {
        _commands = commands;
        _logger = logger;
        eventBus.Subscribe<CommandReceived>(HandleCommandReceived);
    }

    private async Task HandleCommandReceived(CommandReceived evt)
    {
        try
        {
            var doc = DroneCommandDocument.From(evt.Command, evt.TimeStamp);
            await _commands.InsertOneAsync(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write drone command for DroneId {DroneId}", evt.Command.DroneId);
        }
    }
}