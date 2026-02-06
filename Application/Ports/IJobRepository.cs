using Model;

namespace Application.Ports;

public interface IJobRepository
{
    void Save(BackupJob job);
    void Delete(int id);
    List<BackupJob> GetAll();
    BackupJob? GetById(int id);
    void Update(BackupJob job);
    int Count();
}
