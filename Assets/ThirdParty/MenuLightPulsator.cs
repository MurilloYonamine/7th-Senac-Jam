using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Seventh.View.Menu
{
    public class MenuLightPulsator : MonoBehaviour
    {
        [Header("Configurações de Intensidade")]
        [Tooltip("Intensidade mínima da luz 2D")]
        [SerializeField] private float _minIntensity = 0.5f;
        [Tooltip("Intensidade máxima da luz 2D")]
        [SerializeField] private float _maxIntensity = 1.5f;
        
        [Header("Configurações de Tempo")]
        [Tooltip("Velocidade da pulsação")]
        [SerializeField] private float _pulseSpeed = 2f;
        [Tooltip("Deslocamento de tempo para não pulsar junto com outras luzes")]
        [SerializeField] private float _timeOffset = 0f;

        private Light2D _light2D;

        private void Awake()
        {
            _light2D = GetComponent<Light2D>();
        }

        private void Update()
        {
            if (_light2D == null) return;

            float wave = Mathf.Sin(Time.time * _pulseSpeed + _timeOffset);
            float t = (wave + 1f) * 0.5f;
            float targetIntensity = Mathf.Lerp(_minIntensity, _maxIntensity, t);

            _light2D.intensity = targetIntensity;
        }
    }
}
