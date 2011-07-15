using Beavers.Encounter.Core;

namespace Beavers.Encounter.ApplicationServices
{
    public interface ITaskDispatcherFactory
    {
        ITaskDispatcher CrearteDispatcher(Game game);
    }
}