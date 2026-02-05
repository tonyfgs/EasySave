using Application.DTOs;

namespace Application.Ports;

public interface ITransferLogger
{
    void LogTransfer(TransferLog log);
}
