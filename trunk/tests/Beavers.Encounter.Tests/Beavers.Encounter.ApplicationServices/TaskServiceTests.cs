using System;
using System.Linq;
using Beavers.Encounter.ApplicationServices;
using Beavers.Encounter.Core;
using NUnit.Framework;
using Rhino.Mocks;
using SharpArch.Core;
using SharpArch.Core.PersistenceSupport;

namespace Tests.Beavers.Encounter.ApplicationServices
{
    [TestFixture]
    public class TaskServiceTests
    {
        private MockRepository mocks;
        private IRepository<Task> taskRepository;
        private IRepository<TeamTaskState> teamTaskStateRepository;
        private IRepository<AcceptedCode> acceptedCodeRepository;
        private IRepository<AcceptedBadCode> acceptedBadCodeRepository;
        private IRepository<AcceptedTip> acceptedTipRepository;
        private ITaskDispatcherFactory dispatcherFactory;
        private ITaskDispatcher taskDispatcher;
        private Game game;
        private Team team;

        [SetUp]
        public void Setup()
        {
            mocks = new MockRepository();

            taskRepository = mocks.DynamicMock<IRepository<Task>>();
            teamTaskStateRepository = mocks.DynamicMock<IRepository<TeamTaskState>>();
            acceptedCodeRepository = mocks.DynamicMock<IRepository<AcceptedCode>>();
            acceptedBadCodeRepository = mocks.DynamicMock<IRepository<AcceptedBadCode>>();
            acceptedTipRepository = mocks.DynamicMock<IRepository<AcceptedTip>>();
            dispatcherFactory = mocks.DynamicMock<ITaskDispatcherFactory>();
            taskDispatcher = mocks.DynamicMock<ITaskDispatcher>();

            game = new Game { Name = "Game" };
            team = new Team { Name = "Team" };
        }

        [Test]
        public void CanAssignNewTaskTest()
        {
            var newTask = new Task { Name = "New task" };
            newTask.Tips.Add(new Tip { Name = "Tip 0", SuspendTime = 0 });
            newTask.Tips.Add(new Tip { Name = "Tip 1", SuspendTime = 30 });
            newTask.Tips.Add(new Tip { Name = "Tip 2", SuspendTime = 60 });

            Expect.Call(dispatcherFactory.CrearteDispatcher(game)).Return(taskDispatcher);
            Expect.Call(taskDispatcher.GetNextTaskForTeam(null, null, null)).Return(newTask).IgnoreArguments();
            Expect.Call(teamTaskStateRepository.SaveOrUpdate(null)).IgnoreArguments();
            Expect.Call(acceptedTipRepository.SaveOrUpdate(null)).IgnoreArguments();
            
            mocks.ReplayAll();

            var service = new TaskService(taskRepository, teamTaskStateRepository, acceptedCodeRepository,
                                          acceptedBadCodeRepository, acceptedTipRepository, dispatcherFactory);

            var teamGameState = new TeamGameState{ Game = game, Team = team };
            var oldTask = new Task { Name = "Old task" };

            var recalcDateTime = new DateTime(2011, 1, 1, 21, 0, 0);
            service.AssignNewTask(teamGameState, oldTask, recalcDateTime);

            mocks.VerifyAll();

            Assert.AreEqual(newTask, teamGameState.ActiveTaskState.Task);
            Assert.AreEqual(recalcDateTime, teamGameState.ActiveTaskState.TaskStartTime);
            Assert.AreEqual(1, teamGameState.ActiveTaskState.AcceptedTips.Count());
            Assert.AreEqual(0, teamGameState.AcceptedTasks.Count());
        }

        [Test]
        [ExpectedException(ExpectedException = typeof(PreconditionException))]
        public void CanNotAssignNewTaskTest()
        {
            var service = new TaskService(taskRepository, teamTaskStateRepository, acceptedCodeRepository,
                                          acceptedBadCodeRepository, acceptedTipRepository, dispatcherFactory);

            var oldTask = new Task { Name = "Old task" };
            var teamGameState = new TeamGameState { Game = game, Team = team, ActiveTaskState = new TeamTaskState { Task = oldTask } };

            service.AssignNewTask(teamGameState, oldTask, new DateTime(2011, 1, 1, 21, 0, 0));
        }
    }
}
