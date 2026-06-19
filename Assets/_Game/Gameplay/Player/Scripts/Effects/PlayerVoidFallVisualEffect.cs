using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace Seventh.Gameplay.Player.Effects
{
    public class PlayerVoidFallVisualEffect
    {
        private readonly Transform _playerTransform;
        private readonly Transform _visualTransform;
        private readonly Vector3 _originalVisualScale;
        private readonly Quaternion _originalVisualRotation;
        private readonly float _fallDuration;

        private struct SpriteColorInfo
        {
            public SpriteRenderer Renderer;
            public Color OriginalColor;
        }
        private readonly List<SpriteColorInfo> _spriteColors = new List<SpriteColorInfo>();

        public PlayerVoidFallVisualEffect(
            Transform playerTransform,
            Transform visualTransform,
            Vector3 originalVisualScale,
            Quaternion originalVisualRotation,
            float fallDuration)
        {
            _playerTransform = playerTransform;
            _visualTransform = visualTransform;
            _originalVisualScale = originalVisualScale;
            _originalVisualRotation = originalVisualRotation;
            _fallDuration = fallDuration;

            if (_visualTransform != null)
            {
                var renderers = _visualTransform.GetComponentsInChildren<SpriteRenderer>();
                foreach (var r in renderers)
                {
                    _spriteColors.Add(new SpriteColorInfo { Renderer = r, OriginalColor = r.color });
                }
            }
        }

        public void PlayFall(Collider2D activeVoidCollider, System.Action onComplete)
        {
            if (_visualTransform == null || _playerTransform == null)
            {
                onComplete?.Invoke();
                return;
            }

            _visualTransform.DOKill();
            _playerTransform.DOKill();

            Sequence fallSeq = DOTween.Sequence();

            if (activeVoidCollider != null)
            {
                Vector3 targetCenter = activeVoidCollider.bounds.center;
                targetCenter.z = _playerTransform.position.z;

                Vector3 directionToCenter = (targetCenter - _playerTransform.position).normalized;
                float distanceToCenter = Vector3.Distance(_playerTransform.position, targetCenter);
                float stepDistance = Mathf.Min(distanceToCenter, 0.7f);
                Vector3 targetPosition = _playerTransform.position + directionToCenter * stepDistance;
                targetPosition.z = _playerTransform.position.z;

                // Step 1: Slide into the void
                float slideDuration = 0.22f;
                fallSeq.Append(_playerTransform.DOMove(targetPosition, slideDuration).SetEase(Ease.OutQuad));

                // Step 2: Spin and shrink once inside the void area
                fallSeq.Append(_visualTransform.DORotate(new Vector3(0, 0, 720f), _fallDuration, RotateMode.FastBeyond360).SetEase(Ease.InQuad));
                fallSeq.Join(_visualTransform.DOScale(Vector3.zero, _fallDuration).SetEase(Ease.InQuad));

                foreach (var info in _spriteColors)
                {
                    if (info.Renderer == null) continue;
                    fallSeq.Join(info.Renderer.DOColor(Color.black, _fallDuration * 0.5f).SetEase(Ease.InQuad));
                    fallSeq.Join(info.Renderer.DOFade(0f, _fallDuration).SetEase(Ease.InQuad));
                }
            }
            else
            {
                fallSeq.Append(_visualTransform.DORotate(new Vector3(0, 0, 720f), _fallDuration, RotateMode.FastBeyond360).SetEase(Ease.InQuad));
                fallSeq.Join(_visualTransform.DOScale(Vector3.zero, _fallDuration).SetEase(Ease.InQuad));

                foreach (var info in _spriteColors)
                {
                    if (info.Renderer == null) continue;
                    fallSeq.Join(info.Renderer.DOColor(Color.black, _fallDuration * 0.5f).SetEase(Ease.InQuad));
                    fallSeq.Join(info.Renderer.DOFade(0f, _fallDuration).SetEase(Ease.InQuad));
                }
            }

            fallSeq.OnComplete(() => onComplete?.Invoke());
        }

        public void PlayRespawnFadeIn(System.Action onComplete)
        {
            if (_visualTransform == null)
            {
                onComplete?.Invoke();
                return;
            }

            _visualTransform.DOKill();
            _visualTransform.localScale = Vector3.zero;

            foreach (var info in _spriteColors)
            {
                if (info.Renderer == null) continue;
                info.Renderer.color = info.OriginalColor;
            }

            _visualTransform.DOScale(_originalVisualScale, 0.25f).SetEase(Ease.OutBack).OnComplete(() => onComplete?.Invoke());
        }

        public void ResetVisuals()
        {
            if (_visualTransform != null)
            {
                _visualTransform.DOKill();
                _visualTransform.localScale = _originalVisualScale;
                _visualTransform.localRotation = _originalVisualRotation;
            }
            if (_playerTransform != null)
            {
                _playerTransform.DOKill();
            }

            foreach (var info in _spriteColors)
            {
                if (info.Renderer == null) continue;
                info.Renderer.DOKill();
                info.Renderer.color = info.OriginalColor;
            }
        }

        public void CleanUp()
        {
            if (_visualTransform != null)
            {
                _visualTransform.DOKill();
            }
            if (_playerTransform != null)
            {
                _playerTransform.DOKill();
            }
            foreach (var info in _spriteColors)
            {
                if (info.Renderer != null)
                {
                    info.Renderer.DOKill();
                }
            }
        }
    }
}
