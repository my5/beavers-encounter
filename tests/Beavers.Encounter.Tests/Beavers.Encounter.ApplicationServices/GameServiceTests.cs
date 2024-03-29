﻿using System;
using System.Collections.Generic;
using Beavers.Encounter.ApplicationServices;
using Beavers.Encounter.Core;
using NUnit.Framework;
using Rhino.Mocks;
using SharpArch.Core;
using SharpArch.Core.PersistenceSupport;
using Tests.TestHelpers;

namespace Tests.Beavers.Encounter.ApplicationServices
{
    [TestFixture]
    public class GameServiceTests
    {
        private MockRepository mocks;
        private ITaskService taskService;
        private IRepository<Game> gameRepository;
        private IRepository<TeamGameState> teamGameStateRepository;
        private GameService service;

        private Task task1;
        private Tip task1Tip0;
        private Tip task1Tip1;
        private Tip task1Tip2;

        [SetUp]
        public void SetUp()
        {
            mocks = new MockRepository();
            taskService = mocks.DynamicMock<ITaskService>();
            gameRepository = mocks.DynamicMock<IRepository<Game>>();
            teamGameStateRepository = mocks.DynamicMock<IRepository<TeamGameState>>();

            task1 = new Task();
            task1Tip0 = new Tip { SuspendTime = 0, Task = task1 };
            task1Tip1 = new Tip { SuspendTime = 30, Task = task1 };
            task1Tip2 = new Tip { SuspendTime = 60, Task = task1 };
            task1.Tips.Add(task1Tip0);
            task1.Tips.Add(task1Tip1);
            task1.Tips.Add(task1Tip2);
            task1.Codes.Add(new Code { Name = "1", Task = task1 });

            service = new GameService(gameRepository, teamGameStateRepository, taskService);
        }

        #region Startup

        [Test]
        public void CanStartupTest()
        {
            Expect.Call(gameRepository.GetAll()).Return(new List<Game> { new Game() });

            mocks.ReplayAll();

            Game game = new Game();
            var team = new Team { Game = game };
            team.Users.Add(new User { Team = team });
            game.Teams.Add(team);
            service.StartupGame(game);

            mocks.VerifyAll();

            Assert.AreEqual(GameStates.Startup, game.GameState);
            Assert.IsNotNull(team.TeamGameState);
            Assert.AreEqual(1, team.TeamGameStates.Count);
        }

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно перевести игру в предстартовый режим, когда она находится в режиме Startup.")]
        public void CanStartupFromStartupStateTest()
        {
            service.StartupGame(new Game { GameState = GameStates.Startup });
        }

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно перевести игру в предстартовый режим, когда она находится в режиме Started.")]
        public void CanStartupFromStartedStateTest()
        {
            service.StartupGame(new Game { GameState = GameStates.Started });
        }

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно перевести игру в предстартовый режим, когда она находится в режиме Finished.")]
        public void CanStartupFromFinishedStateTest()
        {
            service.StartupGame(new Game { GameState = GameStates.Finished });
        }

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно перевести игру в предстартовый режим, когда она находится в режиме Cloused.")]
        public void CanStartupFromClousedStateTest()
        {
            service.StartupGame(new Game { GameState = GameStates.Cloused });
        }

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно запустить игру, т.к. уже существует запущенная игра.")]
        public void CanStartupWhenOtheGameStartedTest()
        {
            Expect.Call(gameRepository.GetAll()).Return(new List<Game> { new Game { GameState = GameStates.Started } });
            mocks.ReplayAll();
            service.StartupGame(new Game { GameState = GameStates.Planned });
        }

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно запустить игру, т.к. уже существует запущенная игра.")]
        public void CanStartupWhenOtheGameFinishedTest()
        {
            Expect.Call(gameRepository.GetAll())
                .Return(new List<Game> { new Game { GameState = GameStates.Finished } });
            mocks.ReplayAll();
            service.StartupGame(new Game { GameState = GameStates.Planned });
        }

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно запустить игру, т.к. уже существует запущенная игра.")]
        public void CanStartupWhenOtheGameStartupTest()
        {
            Expect.Call(gameRepository.GetAll()).Return(new List<Game> { new Game { GameState = GameStates.Startup } });
            mocks.ReplayAll();
            service.StartupGame(new Game { GameState = GameStates.Planned });
        }

        [Test]
        public void CanStartupWhenOtheGameClousedOrPlannedTest()
        {
            Expect.Call(gameRepository.GetAll()).Return(new List<Game>
            {
                new Game { GameState = GameStates.Planned },
                new Game { GameState = GameStates.Cloused }
            });
            mocks.ReplayAll();
            service.StartupGame(new Game { GameState = GameStates.Planned });
            mocks.VerifyAll();
        }

        #endregion Startup

        #region CloseGame

        [Test]
        public void CanCloseGameTest()
        {
            Game game = new Game { GameState = GameStates.Finished };
            var team = new Team()
                .CreateTeamGameState(game);

            mocks.ReplayAll();

            service.CloseGame(game);

            mocks.VerifyAll();

            Assert.AreEqual(GameStates.Cloused, game.GameState);
            Assert.IsNull(team.TeamGameState);
            Assert.IsNull(team.Game);
        }

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно закрыть игру, когда она находится в режиме Planned.")]
        public void CanClouseWhenGamePlannedTest()
        {
            service.CloseGame(new Game { GameState = GameStates.Planned });
        }

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно закрыть игру, когда она находится в режиме Startup.")]
        public void CanClouseWhenGameStartupTest()
        {
            service.CloseGame(new Game { GameState = GameStates.Startup });
        }

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно закрыть игру, когда она находится в режиме Started.")]
        public void CanClouseWhenGameStartedTest()
        {
            service.CloseGame(new Game { GameState = GameStates.Started });
        }

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно закрыть игру, когда она находится в режиме Cloused.")]
        public void CanClouseWhenGameClousedTest()
        {
            service.CloseGame(new Game { GameState = GameStates.Cloused });
        }

        #endregion CloseGame

        #region ResetGame

        [Test]
        public void CanResetGameTest()
        {
            Game game = new Game { GameState = GameStates.Finished };
            var team = new Team()
                .CreateTeamGameState(game);

            mocks.ReplayAll();

            service.ResetGame(game);

            mocks.VerifyAll();

            Assert.AreEqual(GameStates.Planned, game.GameState);
            Assert.IsNull(team.TeamGameState);
            Assert.AreEqual(game, team.Game);
        }

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно сбросить состояние игры, когда она находится в режиме Started.")]
        public void CanResetWhenGameStartedTest()
        {
            service.ResetGame(new Game { GameState = GameStates.Started });
        }

        #endregion ResetGame

        #region StartGame

        /*[Test]
        public void CanStartGameTest()
        {
            IGameDemon gameDemon = mocks.DynamicMock<IGameDemon>();
            GameService gameService = new GameService(gameRepository,
                teamRepository, teamGameStateRepository, taskService, gameDemon);

            Game game = new Game { GameState = GameStates.Startup };

            Expect.Call(gameRepository.SaveOrUpdate(game));
            Expect.Call(gameDemon.Start);

            mocks.ReplayAll();

            gameService.StartGame(game);

            mocks.VerifyAll();

            Assert.AreEqual(GameStates.Started, game.GameState);
        }*/

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно перевести игру в рабочий режим, когда она находится в режиме Planned.")]
        public void CanStartWhenGamePlannedTest()
        {
            service.StartGame(new Game { GameState = GameStates.Planned });
        }

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно перевести игру в рабочий режим, когда она находится в режиме Started.")]
        public void CanStartWhenGameStartedTest()
        {
            service.StartGame(new Game { GameState = GameStates.Started });
        }

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно перевести игру в рабочий режим, когда она находится в режиме Finished.")]
        public void CanStartWhenGameFinishedTest()
        {
            service.StartGame(new Game { GameState = GameStates.Finished });
        }

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно перевести игру в рабочий режим, когда она находится в режиме Cloused.")]
        public void CanStartWhenGameClousedTest()
        {
            service.StartGame(new Game { GameState = GameStates.Cloused });
        }

        #endregion StartGame

        #region StopGame

        /*[Test]
        public void CanStopGameTest()
        {
            IGameDemon gameDemon = mocks.DynamicMock<IGameDemon>();
            GameService gameService = new GameService(gameRepository,
                teamRepository, teamGameStateRepository, taskService, gameDemon);

            Game game = new Game { GameState = GameStates.Started };
            Team team = new Team()
                .CreateTeamGameState(game)
                .AssignTask(task1, new DateTime(2010, 1, 1, 21, 0, 0));

            Expect.Call(gameDemon.Stop);
            Expect.Call(gameRepository.SaveOrUpdate(game));
            Expect.Call(
                () => taskService.CloseTaskForTeam(
                    team.TeamGameState.ActiveTaskState,
                    TeamTaskStateFlag.Overtime));
            Expect.Call(teamRepository.SaveOrUpdate(team));
            Expect.Call(teamGameStateRepository.SaveOrUpdate(team.TeamGameState));
            Expect.Call(teamGameStateRepository.DbContext).Return(dbContext);

            mocks.ReplayAll();

            gameService.StopGame(game);

            mocks.VerifyAll();

            Assert.AreEqual(GameStates.Finished, game.GameState);
        }*/

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно остановить игру, когда она находится в режиме Planned.")]
        public void CanStopWhenGamePlannedTest()
        {
            service.StopGame(new Game { GameState = GameStates.Planned }, DateTime.Now);
        }

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно остановить игру, когда она находится в режиме Startup.")]
        public void CanStopWhenGameStartupTest()
        {
            service.StopGame(new Game { GameState = GameStates.Startup }, DateTime.Now);
        }

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно остановить игру, когда она находится в режиме Finished.")]
        public void CanStopWhenGameFinishedTest()
        {
            service.StopGame(new Game { GameState = GameStates.Finished }, DateTime.Now);
        }

        [Test, ExpectedException(ExpectedException = typeof(PreconditionException),
            ExpectedMessage = "Невозможно остановить игру, когда она находится в режиме Cloused.")]
        public void CanStopWhenGameClousedTest()
        {
            service.StopGame(new Game { GameState = GameStates.Cloused }, DateTime.Now);
        }

        #endregion StopGame

        #region Управление заданиями (Делегирование вызовов taskService)

        [Test]
        public void CanTaskServiceSubmitCodeTest()
        {
            DateTime dateTimeNow = DateTime.Now;
            Expect.Call(() => taskService.SubmitCode("1", null, null, dateTimeNow));
            mocks.ReplayAll();
            service.SubmitCode("1", null, null, dateTimeNow);
            mocks.VerifyAll();
        }

        [Test]
        public void CanTaskServiceCloseTaskForTeamTest()
        {
            DateTime recalcTime = DateTime.Now;

            Expect.Call(() => taskService.CloseTaskForTeam(null, TeamTaskStateFlag.Execute, recalcTime));
            mocks.ReplayAll();
            service.CloseTaskForTeam(null, TeamTaskStateFlag.Execute, recalcTime);
            mocks.VerifyAll();
        }

        [Test]
        public void CanTaskServiceAssignNewTaskTest()
        {
            DateTime dateTimeNow = DateTime.Now;
            Expect.Call(() => taskService.AssignNewTask(null, null, dateTimeNow));
            mocks.ReplayAll();
            service.AssignNewTask(null, null, dateTimeNow);
            mocks.VerifyAll();
        }

        [Test]
        public void CanTaskServiceAssignNewTaskTipTest()
        {
            DateTime dateTimeNow = DateTime.Now;
            Expect.Call(() => taskService.AssignNewTaskTip(null, null, dateTimeNow));
            mocks.ReplayAll();
            service.AssignNewTaskTip(null, null, dateTimeNow);
            mocks.VerifyAll();
        }

        [Test]
        public void CanTaskServiceAccelerateTaskTest()
        {
            DateTime dateTimeNow = DateTime.Now;
            Expect.Call(() => taskService.AccelerateTask(null, dateTimeNow));
            mocks.ReplayAll();
            service.AccelerateTask(null, dateTimeNow);
            mocks.VerifyAll();
        }

        [Test]
        public void CanTaskServiceCheckExceededBadCodesTest()
        {
            DateTime dateTimeNow = DateTime.Now;
            Expect.Call(() => taskService.CheckExceededBadCodes(null, dateTimeNow));
            mocks.ReplayAll();
            service.CheckExceededBadCodes(null, dateTimeNow);
            mocks.VerifyAll();
        }

        [Test]
        public void CanTaskServiceGetSuggestTipsTest()
        {
            DateTime recalcTime = DateTime.Now;

            Expect.Call(taskService.GetSuggestTips(null, recalcTime));
            mocks.ReplayAll();
            service.GetSuggestTips(null, recalcTime);
            mocks.VerifyAll();
        }

        #endregion Управление заданиями (Делегирование вызовов taskService)
    }
}
