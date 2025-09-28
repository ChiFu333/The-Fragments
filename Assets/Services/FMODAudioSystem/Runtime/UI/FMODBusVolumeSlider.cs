using UnityEngine;
using UnityEngine.UI;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Простой адаптер для Unity UI Slider, который связывает ползунок с громкостью шины FMOD.
    /// Позволяет управлять громкостью (и опционально сохранять её) без написания кода.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class FMODBusVolumeSlider : MonoBehaviour
    {
        /// <summary>
        /// Путь к шине FMOD (например, <c>bus:/</c>, <c>bus:/SFX</c>, <c>bus:/Music</c>).
        /// </summary>
        [Tooltip("FMOD bus path, e.g., bus:/, bus:/SFX, bus:/Music")]
        public string BusPath = "bus:/";
        /// <summary>
        /// Сохранять ли выбранную громкость в PlayerPrefs через менеджер.
        /// </summary>
        public bool Persist = true;

        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            _slider.minValue = 0f;
            _slider.maxValue = 1f;
            _slider.onValueChanged.AddListener(OnValueChanged);
        }

        private void Start()
        {
            var bus = FMODAudioManager.instance?.FindBus(BusPath);
            if (bus == null) return;
            float vol = bus.GetVolume();
            if (vol >= 0f) _slider.SetValueWithoutNotify(vol);
        }

        private void OnDestroy()
        {
            if (_slider != null) _slider.onValueChanged.RemoveListener(OnValueChanged);
        }

        /// <summary>
        /// Обработчик изменения значения ползунка — применяет громкость к указанной шине.
        /// </summary>
        private void OnValueChanged(float value)
        {
            FMODAudioManager.instance?.SetBusVolume(BusPath, value, Persist);
        }
    }
}
