using Beavers.Encounter.Core;

namespace Beavers.Encounter.ApplicationServices
{
    public class TaskDispatcherFactory : ITaskDispatcherFactory
    {
        public TaskDispatcherFactory()
        {
        }

        public ITaskDispatcher CrearteDispatcher(Game game)
        {
            // В зависимости от типа игры нужно создать конкретный диспетчер задач.
            return new RuleTaskDispatcher();
        }
    }
}
