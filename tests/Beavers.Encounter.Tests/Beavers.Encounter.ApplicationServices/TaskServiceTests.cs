using System;
using System.Collections.Generic;
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
        public void CanAssignNewTaskTipTest()
        {
            DateTime recalcTime = DateTime.Now;

            Expect.Call(acceptedTipRepository.SaveOrUpdate(null)).IgnoreArguments();

            mocks.ReplayAll();

            var service = new TaskService(taskRepository, teamTaskStateRepository, acceptedCodeRepository,
                                          acceptedBadCodeRepository, acceptedTipRepository, dispatcherFactory);

            var teamTaskState = new TeamTaskState { TeamGameState = new TeamGameState { Game = game, Team = team } };

            var tip = new Tip();
            service.AssignNewTaskTip(teamTaskState, tip, recalcTime);

            mocks.VerifyAll();

            Assert.AreEqual(1, teamTaskState.AcceptedTips.Count());
            Assert.AreEqual(recalcTime, teamTaskState.AcceptedTips[0].AcceptTime);
            Assert.AreEqual(tip, teamTaskState.AcceptedTips[0].Tip);
            Assert.AreEqual(teamTaskState, teamTaskState.AcceptedTips[0].TeamTaskState);
        }

        [Test]
        public void CanAssignSecondTaskTipTest()
        {
            DateTime recalcTime = DateTime.Now;

            Expect.Call(acceptedTipRepository.SaveOrUpdate(null)).IgnoreArguments();

            mocks.ReplayAll();

            var service = new TaskService(taskRepository, teamTaskStateRepository, acceptedCodeRepository,
                                          acceptedBadCodeRepository, acceptedTipRepository, dispatcherFactory);

            var teamTaskState = new TeamTaskState { TeamGameState = new TeamGameState { Game = game, Team = team } };
            teamTaskState.AcceptedTips.Add(new AcceptedTip { Tip = new Tip { Name = "Tip 0", SuspendTime = 0 } } );
            var tip = new Tip { Name = "Tip 1", SuspendTime = 30 };
            service.AssignNewTaskTip(teamTaskState, tip, recalcTime);

            mocks.VerifyAll();

            Assert.AreEqual(2, teamTaskState.AcceptedTips.Count());
            Assert.AreEqual(recalcTime, teamTaskState.AcceptedTips[1].AcceptTime);
            Assert.AreEqual(tip, teamTaskState.AcceptedTips[1].Tip);
            Assert.AreEqual(teamTaskState, teamTaskState.AcceptedTips[1].TeamTaskState);
        }

        [Test]
        public void ShouldNotAssignDuodleTipTest()
        {
            DateTime recalcTime = DateTime.Now;

            DoNotExpect.Call(acceptedTipRepository.SaveOrUpdate(null));

            mocks.ReplayAll();

            var service = new TaskService(taskRepository, teamTaskStateRepository, acceptedCodeRepository,
                                          acceptedBadCodeRepository, acceptedTipRepository, dispatcherFactory);

            var teamTaskState = new TeamTaskState { TeamGameState = new TeamGameState { Game = game, Team = team } };
            var tip = new Tip { Name = "Tip 0", SuspendTime = 0 };
            teamTaskState.AcceptedTips.Add(new AcceptedTip { Tip = tip });
            service.AssignNewTaskTip(teamTaskState, tip, recalcTime);

            mocks.VerifyAll();

            Assert.AreEqual(1, teamTaskState.AcceptedTips.Count());
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

        [Test]
        public void CanFinishTeamGameTest()
        {
            Expect.Call(dispatcherFactory.CrearteDispatcher(game)).Return(taskDispatcher);
            Expect.Call(taskDispatcher.GetNextTaskForTeam(null, null, null)).Return(null).IgnoreArguments();

            mocks.ReplayAll();

            var service = new TaskService(taskRepository, teamTaskStateRepository, acceptedCodeRepository,
                                          acceptedBadCodeRepository, acceptedTipRepository, dispatcherFactory);

            var teamGameState = new TeamGameState { Game = game, Team = team };
            var oldTask = new Task { Name = "Old task" };

            var recalcDateTime = new DateTime(2011, 1, 1, 21, 0, 0);
            service.AssignNewTask(teamGameState, oldTask, recalcDateTime);

            mocks.VerifyAll();

            Assert.IsNull(teamGameState.ActiveTaskState);
            Assert.AreEqual(recalcDateTime, teamGameState.GameDoneTime);
            Assert.AreEqual(0, teamGameState.AcceptedTasks.Count());
        }

        [Test]
        public void CloseTaskForTeamTest()
        {
            var service = new TaskService(taskRepository, teamTaskStateRepository, acceptedCodeRepository,
                                          acceptedBadCodeRepository, acceptedTipRepository, dispatcherFactory);

            DateTime recalcTime = DateTime.Now;
            var teamTaskState = new TeamTaskState { TeamGameState = new TeamGameState { Game = game, Team = team } };
            service.CloseTaskForTeam(teamTaskState, TeamTaskStateFlag.Success, recalcTime);

            Assert.AreEqual(recalcTime, teamTaskState.TaskFinishTime);
            Assert.AreEqual((int)TeamTaskStateFlag.Success, teamTaskState.State);
            Assert.IsNull(teamTaskState.TeamGameState.ActiveTaskState);
            Assert.True(teamTaskState.TeamGameState.AcceptedTasks.Contains(teamTaskState));
        }

        [Test]
        public void TeamFinishGameTest()
        {
            var service = new TaskService(taskRepository, teamTaskStateRepository, acceptedCodeRepository,
                                          acceptedBadCodeRepository, acceptedTipRepository, dispatcherFactory);

            DateTime recalcTime = DateTime.Now;
            var teamGameState = new TeamGameState { Game = game, Team = team };
            service.TeamFinishGame(teamGameState, recalcTime);

            Assert.AreEqual(recalcTime, teamGameState.GameDoneTime);
            Assert.IsNull(teamGameState.ActiveTaskState);
        }

        [Test]
        public void CanGetCodesFlat()
        {
            IList<string> list = TaskService.GetCodes(" 14D245 , 14b123 14d789 , ", "14d", "14B");

            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("245", list[0]);
            Assert.AreEqual("123", list[1]);
            Assert.AreEqual("789", list[2]);
        }

        [Test]
        public void CanGetCodesWithoutBonusPrefixFlat()
        {
            IList<string> list = TaskService.GetCodes(" 14D245 , 14d123 14d789 , ", "14d", null);

            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("245", list[0]);
            Assert.AreEqual("123", list[1]);
            Assert.AreEqual("789", list[2]);
        }
    }
}
