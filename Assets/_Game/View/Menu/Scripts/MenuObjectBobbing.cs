using UnityEngine;

namespace Seventh.View.Menu
{
    public class MenuObjectBobbing : MonoBehaviour
    {
        [Header("Bobbing de Posição (Flutuação)")]
        [Tooltip("Ativa/desativa o movimento de flutuação espacial")]
        [SerializeField] private bool _enablePositionBobbing = true;
        [Tooltip("Velocidade do movimento em cada eixo (X, Y, Z)")]
        [SerializeField] private Vector3 _positionSpeed = new Vector3(1.5f, 2f, 0f);
        [Tooltip("Amplitude (distância máxima) do movimento em cada eixo (X, Y, Z)")]
        [SerializeField] private Vector3 _positionAmplitude = new Vector3(0.05f, 0.15f, 0f);

        [Header("Bobbing de Rotação (Balanço)")]
        [Tooltip("Ativa/desativa um balanço rotacional suave")]
        [SerializeField] private bool _enableRotationBobbing = false;
        [Tooltip("Velocidade da rotação/inclinação")]
        [SerializeField] private float _rotationSpeed = 1f;
        [Tooltip("Amplitude máxima da inclinação em graus")]
        [SerializeField] private float _rotationAmplitude = 2f;

        [Header("Bobbing de Escala (Respiração)")]
        [Tooltip("Ativa/desativa um efeito de respiração na escala do objeto")]
        [SerializeField] private bool _enableScaleBobbing = false;
        [Tooltip("Velocidade do pulso de escala")]
        [SerializeField] private float _scaleSpeed = 1f;
        [Tooltip("Amplitude máxima da mudança de escala (+/- deste valor)")]
        [SerializeField] private float _scaleAmplitude = 0.02f;

        [Header("Configurações de Atraso (Offset)")]
        [Tooltip("Usa um atraso aleatório inicial para que os objetos não se mexam de forma sincronizada")]
        [SerializeField] private bool _randomizeStartOffset = true;
        [Tooltip("Atraso personalizado manual (caso randomize esteja desativado)")]
        [SerializeField] private float _customOffset = 0f;

        private Vector3 _startPosition;
        private Vector3 _startScale;
        private Quaternion _startRotation;
        private float _offset;

        private void Start()
        {
            _startPosition = transform.localPosition;
            _startScale = transform.localScale;
            _startRotation = transform.localRotation;

            if (_randomizeStartOffset)
            {
                _offset = Random.Range(0f, 100f);
            }
            else
            {
                _offset = _customOffset;
            }
        }

        private void Update()
        {
            float time = Time.time + _offset;

            if (_enablePositionBobbing)
            {
                float offsetX = Mathf.Sin(time * _positionSpeed.x) * _positionAmplitude.x;
                float offsetY = Mathf.Cos(time * _positionSpeed.y) * _positionAmplitude.y;
                float offsetZ = Mathf.Sin(time * _positionSpeed.z) * _positionAmplitude.z;
                transform.localPosition = _startPosition + new Vector3(offsetX, offsetY, offsetZ);
            }

            if (_enableRotationBobbing)
            {
                float angle = Mathf.Sin(time * _rotationSpeed) * _rotationAmplitude;
                transform.localRotation = _startRotation * Quaternion.Euler(0f, 0f, angle);
            }

            if (_enableScaleBobbing)
            {
                float scaleOffset = Mathf.Sin(time * _scaleSpeed) * _scaleAmplitude;
                transform.localScale = _startScale + new Vector3(scaleOffset, scaleOffset, scaleOffset);
            }
        }
    }
}
