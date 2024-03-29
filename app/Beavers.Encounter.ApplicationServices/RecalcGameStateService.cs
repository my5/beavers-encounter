﻿using System;
using System.Linq;
using SharpArch.Core;
using SharpArch.Core.PersistenceSupport;
using Beavers.Encounter.Core;

namespace Beavers.Encounter.ApplicationServices
{
    public class RecalcGameStateService : IRecalcGameStateService
    {
        private readonly int gameId;
        private readonly IGameService gameService;
        private readonly IRepository<Game> gameRepository;

        public RecalcGameStateService(int gameId, IRepository<Game> gameRepository, IGameService gameService)
        {
            Check.Require(gameRepository != null, "gameRepository may not be null");
            Check.Require(gameService != null, "gameService may not be null");

            this.gameId = gameId;
            this.gameRepository = gameRepository;
            this.gameService = gameService;
        }

        public int GameId
        {
            get { return gameId; }
        }

        public void RecalcGameState(DateTime recalcDateTime)
        {
            Game game = gameRepository.Get(GameId);
            if (game.GameState != GameStates.Started)
                return;

            var dbContext = gameRepository.DbContext;
            dbContext.BeginTransaction();
            try
            {

                CheckForGameFinish(game, recalcDateTime);
                foreach (Team team in game.Teams)
                {
                    if (team.TeamGameState != null)
                    {
                        CheckForFirstTask(team.TeamGameState, recalcDateTime);
                        CheckOvertime(team.TeamGameState, recalcDateTime);
                        CheckExceededBadCodes(team.TeamGameState, recalcDateTime);
                        CheckForNextTip(team.TeamGameState, recalcDateTime);
                    }
                }

                dbContext.CommitTransaction();
            }
            catch
            {
                dbContext.RollbackTransaction();
            }
        }

        private void CheckForGameFinish(Game game, DateTime recalcDateTime)
        {
            if ((recalcDateTime - game.GameDate).TotalMinutes >= game.TotalTime && game.GameState == GameStates.Started)
            {
                gameService.StopGame(game, recalcDateTime);
            }
        }

        /// <summary>
        /// Проверка на необходимость выдачи первого задания.
        /// </summary>
        private void CheckForFirstTask(TeamGameState teamGameState, DateTime recalcDateTime)
        {
            if (teamGameState.Game.GameDate > recalcDateTime)
                return;

            if (teamGameState.AcceptedTasks.Count == 0 && teamGameState.ActiveTaskState == null)
            {
                gameService.AssignNewTask(teamGameState, null, recalcDateTime);
            }
        }

        /// <summary>
        /// Проверка на необходимость выдачи подсказки.
        /// </summary>
        /// <param name="teamGameState"></param>
        /// <param name="recalcDateTime"></param>
        private void CheckForNextTip(TeamGameState teamGameState, DateTime recalcDateTime)
        {
            if (teamGameState == null || teamGameState.ActiveTaskState == null)
                return;

            // Для заданий с выбором подсказок ничерта не делаем
            if (teamGameState.ActiveTaskState.Task.TaskType == TaskTypes.RussianRoulette)
                return;

            // время выполнения задания
            TimeSpan taskTime = recalcDateTime - teamGameState.ActiveTaskState.TaskStartTime;

            foreach (Tip tip in teamGameState.ActiveTaskState.Task.Tips)
            {
                if (taskTime.TotalMinutes >= tip.SuspendTime)
                {
                    // если подсказка еще не получена
                    if (!teamGameState.ActiveTaskState.AcceptedTips.Any(t => t.Tip == tip))
                    {
                        // отправляем команде подсказку
                        gameService.AssignNewTaskTip(teamGameState.ActiveTaskState, tip, recalcDateTime);
                    }
                }
            }
        }

        /// <summary>
        /// Проверка на превышение количества левых кодов. При превышении задание закрывается сразу перед первой подсказкой.
        /// </summary>
        /// <param name="teamGameState"></param>
        /// <param name="recalcDateTime"></param>
        private void CheckExceededBadCodes(TeamGameState teamGameState, DateTime recalcDateTime)
        {
            gameService.CheckExceededBadCodes(teamGameState, recalcDateTime);
        }

        /// <summary>
        /// Проверка на перебор времени по заданию.
        /// </summary>
        /// <param name="teamGameState"></param>
        /// <param name="recalcDateTime"></param>
        private void CheckOvertime(TeamGameState teamGameState, DateTime recalcDateTime)
        {
            if (teamGameState == null || teamGameState.ActiveTaskState == null)
                return;

            // время выполнения задания
            TimeSpan taskTime = recalcDateTime - teamGameState.ActiveTaskState.TaskStartTime;
            int timePerTask = teamGameState.Game.TimePerTask;

            // Если задание с "ускорением" и "ускорение" произошло
            if (teamGameState.ActiveTaskState.Task.TaskType == TaskTypes.NeedForSpeed &&
                teamGameState.ActiveTaskState.AccelerationTaskStartTime != null)
            {
                taskTime = recalcDateTime - (DateTime)teamGameState.ActiveTaskState.AccelerationTaskStartTime;
                timePerTask = teamGameState.Game.TimePerTask - teamGameState.ActiveTaskState.Task.Tips.Last(tip => tip.SuspendTime > 0).SuspendTime;
            }

            if (taskTime.TotalMinutes >= timePerTask)
            {
                // Если все основные коды приняты, то задание считаем выполненым успешно
                TeamTaskStateFlag closeFlag =
                    teamGameState.ActiveTaskState.AcceptedCodes.Count(x => !x.Code.IsBonus) == teamGameState.ActiveTaskState.Task.Codes.Count(x => !x.IsBonus)
                    ? TeamTaskStateFlag.Success
                    : TeamTaskStateFlag.Overtime;

                Task oldTask = teamGameState.ActiveTaskState.Task;
                gameService.CloseTaskForTeam(teamGameState.ActiveTaskState, closeFlag, recalcDateTime);
                gameService.AssignNewTask(teamGameState, oldTask, recalcDateTime);
            }
        }
    }
}
