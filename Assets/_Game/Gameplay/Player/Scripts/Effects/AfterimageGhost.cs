using UnityEngine;
using DG.Tweening;

namespace Seventh.Gameplay.Player
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class AfterimageGhost : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;

        public void Init(Sprite playerSprite, Vector3 position, Quaternion rotation, Vector2 scale, Color color, float fadeDuration, int sortingOrder, string sortingLayerName)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            
            transform.position = position;
            transform.rotation = rotation;
            transform.localScale = scale;
            
            _spriteRenderer.sprite = playerSprite;
            _spriteRenderer.color = color;
            _spriteRenderer.sortingOrder = sortingOrder;
            _spriteRenderer.sortingLayerName = sortingLayerName;

            _spriteRenderer.DOFade(0f, fadeDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() => Destroy(gameObject));
        }
    }
}