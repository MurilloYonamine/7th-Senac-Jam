using UnityEngine;

namespace Seventh.Gameplay.Enemy
{
    public class CowardFleeState : IState
    {
        private readonly CowardEnemyController _controller;
        private readonly EnemyStateMachine _stateMachine;
        private float _timeSinceLastShot;

        public CowardFleeState(CowardEnemyController controller, EnemyStateMachine stateMachine)
        {
            _controller = controller;
            _stateMachine = stateMachine;
        }

        public void Enter()
        {
            _timeSinceLastShot = 0f;
        }

        public void Execute()
        {
            if (_controller.PlayerTransform == null) return;

            _timeSinceLastShot += Time.deltaTime;

            Vector2 toPlayer = _controller.PlayerTransform.position - _controller.transform.position;
            float distance = toPlayer.magnitude;

            AimWeaponIn4Directions(toPlayer);

            // Se o player está mais perto que a safe distance, fuja!
            if (distance < _controller.SafeDistance)
            {
                Vector2 fleeDirection = -toPlayer.normalized;
                _controller.HopMovement.Move(fleeDirection);
            }
            else
            {
                // Se está longe o suficiente, pare de fugir
                _controller.HopMovement.Stop();

                // Se o cooldown acabou, mude para o estado de tiro
                if (_timeSinceLastShot >= _controller.ShootCooldown)
                {
                    _stateMachine.ChangeState(new CowardShootState(_controller, _stateMachine));
                }
            }
        }

        public void Exit()
        {
            _controller.HopMovement.Stop();
        }

        private void AimWeaponIn4Directions(Vector2 toPlayer)
        {
            if (_controller.WeaponPivot == null) return;

            Vector2 aimDirection;

            if (Mathf.Abs(toPlayer.x) > Mathf.Abs(toPlayer.y))
            {
                aimDirection = toPlayer.x > 0 ? Vector2.right : Vector2.left;
            }
            else
            {
                aimDirection = toPlayer.y > 0 ? Vector2.up : Vector2.down;
            }

            _controller.WeaponPivot.right = aimDirection;
        }
    }
}
