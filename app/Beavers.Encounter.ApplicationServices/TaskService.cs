using System;
using System.Collections.Generic;
using System.Linq;
using Beavers.Encounter.Core;
using Beavers.Encounter.Core.DataInterfaces;
using SharpArch.Core;
using SharpArch.Core.PersistenceSupport;

namespace Beavers.Encounter.ApplicationServices
{
    public class TaskService : ITaskService
    {
        private readonly IRepository<Task> taskRepository;
        private readonly IRepository<TeamTaskState> teamTaskStateRepository;
        private readonly IRepository<AcceptedCode> acceptedCodeRepository;
        private readonly IRepository<AcceptedBadCode> acceptedBadCodeRepository;
        private readonly IRepository<AcceptedTip> acceptedTipRepository;
        private readonly ITaskDispatcherFactory dispatcherFactory;

        public TaskService(
            IRepository<Task> taskRepository,
            IRepository<TeamTaskState> teamTaskStateRepository,
            IRepository<AcceptedCode> acceptedCodeRepository,
            IRepository<AcceptedBadCode> acceptedBadCodeRepository,
            IRepository<AcceptedTip> acceptedTipRepository,
            ITaskDispatcherFactory dispatcherFactory)
        {
            Check.Require(taskRepository != null, "taskRepository may not be null");
            Check.Require(teamTaskStateRepository != null, "teamTaskStateRepository may not be null");
            Check.Require(acceptedCodeRepository != null, "acceptedCodeRepository may not be null");
            Check.Require(acceptedBadCodeRepository != null, "acceptedBadCodeRepository may not be null");
            Check.Require(acceptedTipRepository != null, "acceptedTipRepository may not be null");
            Check.Require(dispatcherFactory != null, "dispatcherFactory may not be null");

            this.taskRepository = taskRepository;
            this.teamTaskStateRepository = teamTaskStateRepository;
            this.acceptedCodeRepository = acceptedCodeRepository;
            this.acceptedBadCodeRepository = acceptedBadCodeRepository;
            this.acceptedTipRepository = acceptedTipRepository;
            this.dispatcherFactory = dispatcherFactory;
        }

        public void AssignNewTaskTip(TeamTaskState teamTaskState, Tip tip, DateTime dateTimeNow)
        {
            AcceptedTip acceptedTip = new AcceptedTip
                {
                  AcceptTime = dateTimeNow,
                  Tip = tip,
                  TeamTaskState = teamTaskState
                };

            //Подсказку дадим только в том случае, если такая еще не выдавалась.
            if (!teamTaskState.AcceptedTips.Any(x => x.Tip.Id == tip.Id))
            {
                teamTaskState.AcceptedTips.Add(acceptedTip);
            }
            acceptedTipRepository.SaveOrUpdate(acceptedTip);
        }

        public void AssignNewTask(TeamGameState teamGameState, Task oldTask, DateTime dateTimeNow)
        {
            Check.Require(teamGameState.ActiveTaskState == null, "Невозможно назначить команде новую задачу, т.к. коменде уже назначена задача.");

            // Пытаемся получить следующее задание для команды
            Task newTask = dispatcherFactory.CrearteDispatcher(teamGameState.Game)
                .GetNextTaskForTeam(taskRepository, teamGameState, oldTask);
            
            // Если нет нового задания, то команда завершила игру
            if (newTask == null)
            {
                TeamFinishGame(teamGameState);
                return;
            }

            TeamTaskState teamTaskState = new TeamTaskState {
                    TaskStartTime = dateTimeNow, 
                    TaskFinishTime = null,
                    State = (int) TeamTaskStateFlag.Execute,
                    TeamGameState = teamGameState,
                    Task = newTask,
                    NextTask = null
                };

            teamGameState.ActiveTaskState = teamTaskState;

            teamTaskStateRepository.SaveOrUpdate(teamTaskState);
            //Сразу же отправляем команде первую подсказку (т.е. текст задания)
            AssignNewTaskTip(teamTaskState, teamTaskState.Task.Tips.First(), dateTimeNow);
        }

        public void CloseTaskForTeam(TeamTaskState teamTaskState, TeamTaskStateFlag flag)
        {
            teamTaskState.TaskFinishTime = DateTime.Now;
            teamTaskState.State = (int) flag;
            
            teamTaskState.TeamGameState.ActiveTaskState = null;
            teamTaskState.TeamGameState.AcceptedTasks.Add(teamTaskState);
        }

        public void TeamFinishGame(TeamGameState teamGameState)
        {
            teamGameState.GameDoneTime = DateTime.Now;
            teamGameState.ActiveTaskState = null;
        }

        /// <summary>
        /// "Ускориться".
        /// </summary>
        /// <remarks>
        /// Устанавливает время ускорения в текущее и назначает вторую подсказку.
        /// </remarks>
        /// <param name="teamTaskState">Состояние команды затребовавшая ускорение.</param>
        /// <param name="dateTimeNow"></param>
        public void AccelerateTask(TeamTaskState teamTaskState, DateTime dateTimeNow)
        {
            teamTaskState.AccelerationTaskStartTime = DateTime.Now;
            AssignNewTaskTip(teamTaskState, teamTaskState.Task.Tips.Last(tip => tip.SuspendTime > 0), dateTimeNow);
        }

        /// <summary>
        /// Возвращает варианты выбора подсказок, если это необходимо для задания с выбором подсказки.
        /// </summary>
        public IEnumerable<Tip> GetSuggestTips(TeamTaskState teamTaskState)
        {
            // Время от начала задания
            double taskTimeSpend = (DateTime.Now - teamTaskState.TaskStartTime).TotalMinutes;
            // Время получения последней из полученных подсказок
            double lastAcceptTipTime = (teamTaskState.AcceptedTips.Last().AcceptTime - teamTaskState.TaskStartTime).TotalMinutes;
            // Подсказки, 
            // которые дожны быть выданы на данный момент, 
            var tips = new List<Tip>(teamTaskState.Task.Tips
                .Where(tip => tip.SuspendTime > lastAcceptTipTime && tip.SuspendTime <= taskTimeSpend && tip.SuspendTime < teamTaskState.TeamGameState.Game.TimePerTask));

            // Если пришло время предложить команде выбрать подсказку
            if (tips.Count() > 0)
            {
                // Все подсказки, исключая уже выданные
                return teamTaskState.Task.Tips
                    .Where(tip => tip.SuspendTime > 0)
                    .Except(teamTaskState.AcceptedTips.Tips());
            }
         
            return null;
        }

        public void SubmitCode(string codes, TeamGameState teamGameState, User user, DateTime dateTimeNow)
        {
            if (teamGameState.ActiveTaskState == null ||
                teamGameState.ActiveTaskState.AcceptedBadCodes.Count >= GameConsnt.BadCodesLimit)
                return;

            List<string> codesList = GetCodes(codes, teamGameState.Game.PrefixMainCode, teamGameState.Game.PrefixBonusCode);
            if (codesList.Count == 0)
                return;

            if (codesList.Count > teamGameState.ActiveTaskState.Task.Codes.Count)
                throw new MaxCodesCountException(String.Format("Запрещено вводить количество кодов, за один раз, большее, чем количество кодов в задании."));

            foreach (Code code in teamGameState.ActiveTaskState.Task.Codes)
            {
                if (codesList.Contains(code.Name.Trim().ToUpper()))
                {
                    codesList.Remove(code.Name.Trim().ToUpper());
                    if (!teamGameState.ActiveTaskState.AcceptedCodes.Any(x => x.Code.Id == code.Id))
                    {
                        // Добавляем правильный принятый код
                        AcceptedCode acceptedCode = new AcceptedCode
                        {
                            AcceptTime = DateTime.Now,
                            Code = code,
                            TeamTaskState = teamGameState.ActiveTaskState
                        };
                        
                        teamGameState.ActiveTaskState.AcceptedCodes.Add(acceptedCode);
                        acceptedCodeRepository.SaveOrUpdate(acceptedCode);
                    }
                }
            }

            // Добавляем некорректные принятые коды
            foreach (string badCode in codesList)
            {
                if (!teamGameState.ActiveTaskState.AcceptedBadCodes.Any(x => x.Name.Trim().ToUpper() == badCode))
                {
                    AcceptedBadCode acceptedBadCode = new AcceptedBadCode
                    {
                        AcceptTime = DateTime.Now,
                        Name = badCode,
                        TeamTaskState = teamGameState.ActiveTaskState
                    };

                    teamGameState.ActiveTaskState.AcceptedBadCodes.Add(acceptedBadCode);
                    acceptedBadCodeRepository.SaveOrUpdate(acceptedBadCode);
                }
            }

            // Если приняты все основные коды, то помечаем задание выполненым и назначаем новое
            if (teamGameState.ActiveTaskState.AcceptedCodes.Count == teamGameState.ActiveTaskState.Task.Codes.Count/*(x => x.IsBonus == 0)*/ &&
                teamGameState.ActiveTaskState.AcceptedCodes.Count > 0)
            {
                Task oldTask = teamGameState.ActiveTaskState.Task;
                CloseTaskForTeam(teamGameState.ActiveTaskState, TeamTaskStateFlag.Success);
                AssignNewTask(teamGameState, oldTask, dateTimeNow);
            }

            CheckExceededBadCodes(teamGameState, dateTimeNow);
        }

        /// <summary>
        /// Проверка на превышение количества левых кодов. При превышении задание закрывается сразу перед первой подсказкой.
        /// </summary>
        public void CheckExceededBadCodes(TeamGameState teamGameState, DateTime dateTimeNow)
        {
            if (teamGameState == null || teamGameState.ActiveTaskState == null)
                return;

            if ((teamGameState.ActiveTaskState.AcceptedBadCodes.Count >= GameConsnt.BadCodesLimit)
                && (((DateTime.Now - teamGameState.ActiveTaskState.TaskStartTime).TotalMinutes + 1) //+1 - чтобы сработало до того, как покажется первая подсказка.
                     >= (teamGameState.ActiveTaskState.Task.Tips.First(x => x.SuspendTime > 0).SuspendTime)))
            {
                Task oldTask = teamGameState.ActiveTaskState.Task;
                CloseTaskForTeam(teamGameState.ActiveTaskState, TeamTaskStateFlag.Cheat);
                AssignNewTask(teamGameState, oldTask, dateTimeNow);
            }
        }

        public static List<string> GetCodes(string codes, string prefixMainCode, string prefixBonusCode)
        {
            var codesList = new List<string>();

            string[] codeParts = codes.Split(new[] { ',', ' ' });
            foreach (string codePart in codeParts)
            {
                string tmpCode = codePart.Trim().ToUpper();
                if (tmpCode.StartsWith(prefixMainCode.ToUpper()))
                {
                    tmpCode = tmpCode.Substring(prefixMainCode.Length);
                }
                else if (!String.IsNullOrEmpty(prefixBonusCode) && tmpCode.StartsWith(prefixBonusCode.ToUpper()))
                {
                    tmpCode = tmpCode.Substring(prefixBonusCode.Length);
                }

                if (!String.IsNullOrEmpty(tmpCode))
                    codesList.Add(tmpCode);
            }
            return codesList;
        }
    }
}
