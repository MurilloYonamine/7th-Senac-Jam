using UnityEngine;
using Seventh.Core.Services;
using Seventh.Gameplay.Environment;

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

            // If the enemy is currently stunned (recovering from hit), stop movement and wait
            var enemyComponent = _controller.GetComponent<Seventh.Gameplay.Enemies.Enemy>();
            if (enemyComponent != null && enemyComponent.IsStunned)
            {
                _controller.HopMovement.Stop();
                return;
            }

            _timeSinceLastShot += Time.deltaTime;

            Vector2 toPlayer = _controller.PlayerTransform.position - _controller.transform.position;
            float distance = toPlayer.magnitude;

            _controller.AimWeapon(toPlayer);

            // Se for variação de tiro, ele não se move (fica parado mirando e atirando)
            if (_controller.CanShoot)
            {
                _controller.HopMovement.Stop();

                if (_timeSinceLastShot >= _controller.ShootCooldown)
                {
                    _stateMachine.ChangeState(new CowardShootState(_controller, _stateMachine));
                }
                return;
            }

            // Variação que apenas foge (não atira):
            if (distance < _controller.SafeDistance)
            {
                Vector2 fleeDirection = -toPlayer.normalized;
                var pathfinder = ServiceLocator.Get<ITilemapPathfinder>();

                if (pathfinder != null)
                {
                    Collider2D roomCollider = _controller.MyRoom != null ? _controller.MyRoom.RoomCollider : null;
                    Vector3 targetPosition = pathfinder.FindBestFleePosition(_controller.transform.position, _controller.PlayerTransform.position, _controller.SafeDistance, roomCollider);
                    var path = pathfinder.FindPath(_controller.transform.position, targetPosition, roomCollider);

                    _controller.SetDebugPath(path, targetPosition);

                    if (path != null && path.Count > 0)
                    {
                        Vector3 nextNode = path.Count > 1 ? path[1] : path[0];
                        
                        // If we are already very close to the first node, pick the next one to maintain smooth flow
                        if (Vector3.Distance(_controller.transform.position, nextNode) < 0.2f && path.Count > 2)
                        {
                            nextNode = path[2];
                        }

                        Vector2 nextStepDir = (nextNode - _controller.transform.position).normalized;
                        _controller.HopMovement.Move(nextStepDir);
                    }
                    else
                    {
                        _controller.HopMovement.Move(fleeDirection);
                    }
                }
                else
                {
                    _controller.HopMovement.Move(fleeDirection);
                }
            }
            else
            {
                _controller.HopMovement.Stop();
            }
        }

        public void Exit()
        {
            _controller.HopMovement.Stop();
        }
    }
}
