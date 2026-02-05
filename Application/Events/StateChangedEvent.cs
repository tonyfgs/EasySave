using Application.DTOs;

namespace Application.Events;

public record StateChangedEvent(StateSnapshot Snapshot);
