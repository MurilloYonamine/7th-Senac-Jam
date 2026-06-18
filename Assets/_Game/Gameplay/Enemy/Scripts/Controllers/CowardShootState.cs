using UnityEngine;

namespace Seventh.Gameplay.Enemy
{
    public class CowardShootState : IState
    {
        private readonly CowardEnemyController _controller;
        private readonly EnemyStateMachine _stateMachine;
        private float _shootTimer;
        private bool _hasShot;

        public CowardShootState(CowardEnemyController controller, EnemyStateMachine stateMachine)
        {
            _controller = controller;
            _stateMachine = stateMachine;
        }

        public void Enter()
        {
            _shootTimer = _controller.ShootStateDuration;
            _hasShot = false;
            
            // Para de se mover para atirar
            _controller.HopMovement.Stop();
        }

        public void Execute()
        {
            if (_controller.PlayerTransform == null) return;

            Vector2 toPlayer = _controller.PlayerTransform.position - _controller.transform.position;
            float distance = toPlayer.magnitude;

            _controller.AimWeapon(toPlayer);

            // Se o player se aproximar demais enquanto se preparava pra atirar, aborta e foge (apenas se for variação que foge)
            if (!_controller.CanShoot && distance < _controller.SafeDistance * 0.8f)
            {
                _stateMachine.ChangeState(new CowardFleeState(_controller, _stateMachine));
                return;
            }

            _shootTimer -= Time.deltaTime;
            
            // Atira mais ou menos no meio da animação/estado
            if (!_hasShot && _shootTimer <= _controller.ShootStateDuration / 2f)
            {
                _hasShot = true;
                _controller.TriggerShoot();
            }

            // Terminou o tempo do estado de tiro, volta a avaliar
            if (_shootTimer <= 0f)
            {
                _stateMachine.ChangeState(new CowardFleeState(_controller, _stateMachine));
            }
        }

        public void Exit()
        {
        }
    }
}
