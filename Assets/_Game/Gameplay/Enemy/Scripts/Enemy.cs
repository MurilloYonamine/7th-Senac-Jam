using UnityEngine;

namespace Seventh.Gameplay.Enemies
{
    public class Enemy : MonoBehaviour
    {
        public virtual void TakeHit(int comboStep, Vector2 attackDirection)
        {
            Debug.Log($"{gameObject.name} was hit by combo strike {comboStep} from direction {attackDirection}");
        }
    }
}
