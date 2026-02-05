using Model;

namespace Application.DTOs;

public record JobExecutionResult(int JobId, BackupResult Result);
