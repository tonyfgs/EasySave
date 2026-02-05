namespace Model;

public class InvalidBackupJobException : DomainException
{
    public InvalidBackupJobException(string reason) : base(reason) { }
}
