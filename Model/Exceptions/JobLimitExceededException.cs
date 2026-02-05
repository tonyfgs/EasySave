namespace Model;

public class JobLimitExceededException : DomainException
{
    public JobLimitExceededException(int max)
        : base($"Job limit exceeded. Maximum allowed: {max}") { }
}
