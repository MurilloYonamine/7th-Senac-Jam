using Seventh.Core.Constants;

namespace Seventh.Core.Services
{
    public interface IGameStateService
    {
        GameState CurrentGameState { get; }

        void ChangeGameState(GameState newState);
    }
}
