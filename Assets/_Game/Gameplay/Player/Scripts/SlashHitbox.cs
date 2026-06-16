using UnityEngine;
using Seventh.Gameplay.Enemies;

namespace Seventh.Gameplay.Player
{
    [RequireComponent(typeof(Collider2D))]
    public class SlashHitbox : MonoBehaviour
    {
        private int _comboStep = 1;
        public void Initialize(int comboStep)
        {
            _comboStep = comboStep;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                // The VFX transform.right points exactly in the direction of the slash attack
                Vector2 pushDirection = transform.right;
                enemy.TakeHit(_comboStep, pushDirection);
            }
        }
    }
}
