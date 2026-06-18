namespace Seventh.Gameplay.Enemy
{
    public interface IState
    {
        void Enter();
        void Execute();
        void Exit();
    }

    public class EnemyStateMachine
    {
        private IState _currentState;

        public void ChangeState(IState newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();
        }

        public void Update()
        {
            _currentState?.Execute();
        }
    }
}
