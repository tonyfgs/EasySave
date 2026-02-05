using Application.DTOs;
using Application.Events;
using Application.Ports;

namespace Application.Handlers;

public class TransferCompletedHandler : IEventHandler<TransferCompletedEvent>
{
    private readonly ITransferLogger _logger;

    public TransferCompletedHandler(ITransferLogger logger)
    {
        _logger = logger;
    }

    public void Handle(TransferCompletedEvent @event)
    {
        _logger.LogTransfer(@event.Transfer);
    }
}
