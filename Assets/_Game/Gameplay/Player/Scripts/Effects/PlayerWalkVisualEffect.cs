using UnityEngine;
using DG.Tweening;

namespace Seventh.Gameplay.Player.Effects
{
    public class PlayerWalkVisualEffect
    {
        private readonly Transform _visualModel;
        private readonly bool _useBobbing;
        private readonly float _bobHeight;
        private readonly float _bobDuration;
        private readonly bool _useSquashAndStretch;
        private readonly Vector2 _squashScale;

        private Sequence _walkSequence;
        private Vector3 _initialLocalPosition;
        private Vector3 _initialLocalScale;
        private bool _isAnimating;

        public PlayerWalkVisualEffect(
            Transform visualModel,
            bool useBobbing,
            float bobHeight,
            float bobDuration,
            bool useSquashAndStretch,
            Vector2 squashScale)
        {
            _visualModel = visualModel;
            _useBobbing = useBobbing;
            _bobHeight = bobHeight;
            _bobDuration = bobDuration;
            _useSquashAndStretch = useSquashAndStretch;
            _squashScale = squashScale;

            if (_visualModel != null)
            {
                _initialLocalPosition = _visualModel.localPosition;
                _initialLocalScale = _visualModel.localScale;
            }
        }

        public void Play()
        {
            if (_isAnimating || _visualModel == null) return;
            _isAnimating = true;
            PlayWalkStep();
        }

        private void PlayWalkStep()
        {
            if (!_isAnimating || _visualModel == null) return;

            _walkSequence = DOTween.Sequence();

            if (_useBobbing)
            {
                _walkSequence.Append(_visualModel.DOLocalMoveY(_initialLocalPosition.y + _bobHeight, _bobDuration / 2f).SetEase(Ease.OutQuad));
                _walkSequence.Append(_visualModel.DOLocalMoveY(_initialLocalPosition.y, _bobDuration / 2f).SetEase(Ease.InQuad));
            }

            if (_useSquashAndStretch)
            {
                Sequence scaleSeq = DOTween.Sequence();
                scaleSeq.Append(_visualModel.DOScale(new Vector3(_initialLocalScale.x * _squashScale.x, _initialLocalScale.y * _squashScale.y, _initialLocalScale.z), _bobDuration * 0.3f).SetEase(Ease.OutQuad));
                scaleSeq.Append(_visualModel.DOScale(_initialLocalScale, _bobDuration * 0.7f).SetEase(Ease.InOutQuad));

                if (_useBobbing)
                {
                    _walkSequence.Join(scaleSeq);
                }
                else
                {
                    _walkSequence.Append(scaleSeq);
                }
            }

            // Fallback se nenhum estiver ativado
            if (!_useBobbing && !_useSquashAndStretch)
            {
                _walkSequence.AppendInterval(_bobDuration);
            }

            _walkSequence.OnComplete(() =>
            {
                PlayWalkStep();
            });
        }

        public void Stop()
        {
            if (!_isAnimating) return;
            _isAnimating = false;
            _walkSequence?.Kill();

            if (_visualModel != null)
            {
                _visualModel.DOKill();
                _visualModel.localPosition = _initialLocalPosition;
                _visualModel.localScale = _initialLocalScale;
            }
        }

        public void CleanUp()
        {
            _walkSequence?.Kill();
        }
    }
}
