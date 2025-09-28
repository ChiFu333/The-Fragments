using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Сервис для работы с шинами: поиск, установка громкости, плавные изменения и дакинг, загрузка/сохранение уровня.
    /// Использует карту шин менеджера и настройки из <see cref="FMODBusServiceAsset"/> для сохранения громкости.
    /// </summary>
    internal sealed class FmodBusService : IBusService
    {
        private readonly FMODAudioManager _manager;
        private readonly FMODAudioSettingsAsset _settingsAsset; // может быть null (for general settings)
        private readonly FMODBusServiceAsset _busSettings; // bus-specific settings

        /// <summary>
        /// Создать сервис шин.
        /// </summary>
        /// <param name="manager">Ссылка на аудио-менеджер.</param>
        /// <param name="settingsAsset">Глобальные настройки аудио (могут быть null).</param>
        /// <param name="busSettings">Актив с настройками шин (пути, хранение громкостей).</param>
        public FmodBusService(FMODAudioManager manager, FMODAudioSettingsAsset settingsAsset, FMODBusServiceAsset busSettings)
        {
            _manager = manager;
            _settingsAsset = settingsAsset;
            _busSettings = busSettings;
        }

        /// <summary>
        /// Найти шину по пути.
        /// </summary>
        public FMODBus FindBus(string busPath) => _manager.FindBus(busPath);

        /// <summary>
        /// Установить громкость шины и, при необходимости, сохранить её в PlayerPrefs.
        /// </summary>
        /// <param name="busPath">Путь к шине (например, <c>bus:/Music</c>).</param>
        /// <param name="volume">Громкость 0..1.</param>
        /// <param name="persist">Сохранять ли значение (если разрешено в настройках сервиса).</param>
        public void SetBusVolume(string busPath, float volume, bool persist)
        {
            var bus = FindBus(busPath);
            if (bus == null) return;
            volume = Mathf.Clamp01(volume);
            bus.SetVolume(volume);
            if (!persist || _busSettings == null || !_busSettings.PersistVolumes) return;
            var init = _busSettings.FindBusInit(busPath);
            if (init == null) return;
            var key = _busSettings.GetBusPrefsKey(init);
            PlayerPrefs.SetFloat(key, volume);
        }

        /// <summary>
        /// Плавно изменить громкость указанной шины.
        /// </summary>
        public UniTask FadeBusVolume(string busPath, float toVolume, float duration)
        {
            var bus = FindBus(busPath);
            if (bus == null) return UniTask.CompletedTask;
            return FadeBus(bus, toVolume, duration);
        }

        /// <summary>
        /// Дакинг (ducking) шины: атака до <paramref name="toVolume"/>, удержание, затем релиз обратно.
        /// </summary>
        public async UniTask DuckBus(string busPath, float toVolume, float attack, float hold, float release)
        {
            var bus = FindBus(busPath);
            if (bus == null) return;
            await DuckBus(bus, toVolume, attack, hold, release);
        }

        /// <summary>
        /// Загрузить сохранённую громкость для указанной шины из PlayerPrefs.
        /// </summary>
        public float LoadBusVolume(FMODBusServiceAsset.BusInit init)
        {
            if (init == null) return -1f;
            if (_busSettings == null) return -1f;
            var key = _busSettings.GetBusPrefsKey(init);
            if (!PlayerPrefs.HasKey(key)) return -1f;
            return PlayerPrefs.GetFloat(key);
        }

        private UniTask FadeBus(FMODBus bus, float toVolume, float duration)
        {
            float startVol = Mathf.Max(0f, bus.GetVolume());
            toVolume = Mathf.Clamp01(toVolume);
            if (duration <= 0f)
            {
                bus.SetVolume(toVolume);
                return UniTask.CompletedTask;
            }
            var tween = DOTween.To(() => startVol, v => bus.SetVolume(v), toVolume, duration);
            return UniTask.WaitUntil(() => !tween.IsActive() || tween.IsComplete());
        }

        private async UniTask DuckBus(FMODBus bus, float toVolume, float attack, float hold, float release)
        {
            float startVol = Mathf.Max(0f, bus.GetVolume());
            toVolume = Mathf.Clamp01(toVolume);

            if (attack > 0f)
            {
                var tweenIn = DOTween.To(() => startVol, v => bus.SetVolume(v), toVolume, attack);
                await UniTask.WaitUntil(() => !tweenIn.IsActive() || tweenIn.IsComplete());
            }
            else bus.SetVolume(toVolume);

            if (hold > 0f) await UniTask.Delay(TimeSpan.FromSeconds(hold));

            if (release > 0f)
            {
                var tweenOut = DOTween.To(() => toVolume, v => bus.SetVolume(v), startVol, release);
                await UniTask.WaitUntil(() => !tweenOut.IsActive() || tweenOut.IsComplete());
            }
            else bus.SetVolume(startVol);
        }
    }
}
