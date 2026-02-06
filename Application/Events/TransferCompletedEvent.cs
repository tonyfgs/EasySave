using Application.DTOs;

namespace Application.Events;

public record TransferCompletedEvent(TransferLog Transfer);
