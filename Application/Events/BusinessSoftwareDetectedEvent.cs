using Application.Ports;

namespace Application.Events;

public record BusinessSoftwareDetectedEvent(
    string JobName,
    BusinessSoftwareStatus Status,
    DateTime Timestamp);
