﻿using System;
using System.Collections.Generic;
using Beavers.Encounter.Core;

namespace Beavers.Encounter.ApplicationServices
{
    public interface ITaskService
    {
        /// <summary>
        /// Обработка принятого кода от команды.
        /// </summary>
        /// <param name="codes">Принятый код.</param>
        /// <param name="teamGameState">Команда отправившая код.</param>
        /// <param name="user">Игрок отправившый код.</param>
        /// <param name="recalcTime"></param>
        void SubmitCode(string codes, TeamGameState teamGameState, User user, DateTime recalcTime);

        /// <summary>
        /// Помечает задание как выполненное, назначает причину завершения.
        /// </summary>
        void CloseTaskForTeam(TeamTaskState teamTaskState, TeamTaskStateFlag flag, DateTime recalcTime);

        /// <summary>
        /// Назначение нового задания команде.
        /// </summary>
        void AssignNewTask(TeamGameState teamGameState, Task oldTask, DateTime recalcTime);

        /// <summary>
        /// Отправить команде подсказку.
        /// </summary>
        /// <param name="teamTaskState"></param>
        /// <param name="tip"></param>
        /// <param name="recalcTime"></param>
        void AssignNewTaskTip(TeamTaskState teamTaskState, Tip tip, DateTime recalcTime);

        /// <summary>
        /// "Ускориться".
        /// </summary>
        /// <param name="teamTaskState">Состояние команды затребовавшая ускорение.</param>
        /// <param name="recalcTime"></param>
        void AccelerateTask(TeamTaskState teamTaskState, DateTime recalcTime);

        /// <summary>
        /// Проверка на превышение количества левых кодов. При превышении задание закрывается сразу перед первой подсказкой.
        /// </summary>
        void CheckExceededBadCodes(TeamGameState teamGameState, DateTime recalcTime);

        /// <summary>
        /// Возвращает варианты выбора подсказок, если это необходимо для задания с выбором подсказки.
        /// </summary>
        IEnumerable<Tip> GetSuggestTips(TeamTaskState teamTaskState, DateTime recalcTime);
    }
}
