using System;

namespace Beavers.Encounter.ApplicationServices
{
    public interface IRecalcGameStateService
    {
        int GameId { get; }
        void RecalcGameState(DateTime recalcDateTime);
    }
}
