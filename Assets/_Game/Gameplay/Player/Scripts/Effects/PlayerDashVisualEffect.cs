using UnityEngine;
using DG.Tweening;

namespace Seventh.Gameplay.Player.Effects
{
    public class PlayerDashVisualEffect
    {
        private readonly SpriteRenderer _spriteRenderer;

        public PlayerDashVisualEffect(SpriteRenderer spriteRenderer)
        {
            _spriteRenderer = spriteRenderer;
        }

        public void ApplySquashAndStretch(Vector3 direction)
        {
            if (_spriteRenderer == null) return;

            Vector3 dashScale = Vector3.one;
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                dashScale = new Vector3(1.3f, 0.7f, 1f);
            }
            else
            {
                dashScale = new Vector3(0.7f, 1.3f, 1f);
            }

            _spriteRenderer.transform.DOKill();
            _spriteRenderer.transform.DOScale(dashScale, 0.05f);
        }

        public void ResetScale()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.transform.DOKill();
                _spriteRenderer.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutElastic);
            }
        }

        public void ResetScaleImmediately()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.transform.DOKill();
                _spriteRenderer.transform.localScale = Vector3.one;
            }
        }

        public void CleanUp()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.transform.DOKill();
            }
        }
    }
}
