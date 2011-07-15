using Beavers.Encounter.Core;
using SharpArch.Core.PersistenceSupport;

namespace Beavers.Encounter.ApplicationServices
{
    public interface ITaskDispatcher
    {
        Task GetNextTaskForTeam(IRepository<Task> taskRepository, TeamGameState teamGameState, Task oldTask);
    }
}
