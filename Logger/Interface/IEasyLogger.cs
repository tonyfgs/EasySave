namespace Logger.Interface;

public interface IEasyLogger
{
    void WriteInFile<T>(string path, T logData);
}
