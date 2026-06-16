using System.Collections;
using UnityEngine;
using Seventh.Gameplay.Health;

namespace Seventh.Gameplay.Enemies
{
    public class DummyHealth : HealthBase
    {
        [Header("Regeneration Settings")]
        [SerializeField] private float _regenDelay = 1.5f;
        [SerializeField] private int _regenRate = 30;
        [SerializeField] private float _regenTick = 0.1f;

        private Coroutine _regenCoroutine;

        public override void TakeDamage(DamageInfo damageInfo)
        {
            int finalDamage = damageInfo.DamageAmount;
            if (CurrentHealth - finalDamage <= 0)
            {
                finalDamage = CurrentHealth - 1;
            }

            if (finalDamage > 0)
            {
                var modifiedInfo = damageInfo;
                modifiedInfo.DamageAmount = finalDamage;
                base.TakeDamage(modifiedInfo);
            }
            else
            {
                var zeroInfo = damageInfo;
                zeroInfo.DamageAmount = 0;
                base.TakeDamage(zeroInfo);
            }

            if (_regenCoroutine != null)
            {
                StopCoroutine(_regenCoroutine);
            }
            _regenCoroutine = StartCoroutine(RegenRoutine());
        }

        private IEnumerator RegenRoutine()
        {
            yield return new WaitForSeconds(_regenDelay);

            while (CurrentHealth < MaxHealth)
            {
                int regenAmount = Mathf.Max(1, Mathf.RoundToInt(_regenRate * _regenTick));
                Heal(regenAmount);
                yield return new WaitForSeconds(_regenTick);
            }

            _regenCoroutine = null;
        }

        private void OnDisable()
        {
            if (_regenCoroutine != null)
            {
                StopCoroutine(_regenCoroutine);
                _regenCoroutine = null;
            }
        }
    }
}
