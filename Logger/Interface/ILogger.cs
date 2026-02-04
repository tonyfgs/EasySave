namespace Logger.Interface;

public interface ILogger
{
    void WriteInFile<T>(string path, T logData);
}