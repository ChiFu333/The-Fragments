using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using STOP_MODE = FMOD.Studio.STOP_MODE;
using UnityEngine;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Сервис событий: создание/поиск контейнеров, управление жизненным циклом (старт/стоп/пауза),
    /// предзагрузка и LRU-эвикция при переполнении кэша. Настройки кэша берутся из <see cref="FMODEventServiceAsset"/>.
    /// </summary>
    internal sealed class FmodEventService : IEventService
    {
        private readonly FMODAudioSettingsAsset _settingsAsset; // может быть null
        private readonly FMODEventServiceAsset _eventSettings; // holds preload and cache settings

        private readonly List<FMODEventContainer> _eventContainers = new();
        private readonly Dictionary<string, FMODEventContainer> _eventsByName = new(StringComparer.Ordinal);
        private readonly Dictionary<Guid, FMODEventContainer> _eventsByGuid = new();
        private readonly Dictionary<FMODEventContainer, float> _lastUsed = new();

        /// <summary>
        /// Создать сервис событий.
        /// </summary>
        /// <param name="settingsAsset">Глобальные настройки аудио (могут быть null).</param>
        /// <param name="eventSettings">Актив с настройками сервиса событий (кэш и предзагрузка).</param>
        public FmodEventService(FMODAudioSettingsAsset settingsAsset, FMODEventServiceAsset eventSettings)
        {
            _settingsAsset = settingsAsset;
            _eventSettings = eventSettings;
        }

        // --- Public queries ---
        /// <summary>
        /// Найти контейнер события по имени контейнера.
        /// </summary>
        public FMODEventContainer FindByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            _eventsByName.TryGetValue(name, out var c);
            return c;
        }
        /// <summary>
        /// Найти контейнер события по ссылке FMOD.
        /// </summary>
        public FMODEventContainer FindByRef(EventReference reference)
        {
            _eventsByGuid.TryGetValue(reference.Guid, out var c);
            return c;
        }
        /// <summary>
        /// Найти контейнер по живому экземпляру <see cref="EventInstance"/>.
        /// </summary>
        public FMODEventContainer FindByInstance(EventInstance instance)
        {
            if (!instance.isValid()) return null;
            for (int i = 0; i < _eventContainers.Count; i++)
            {
                if (_eventContainers[i].EventInstance.Equals(instance)) return _eventContainers[i];
            }
            return null;
        }

        /// <summary>Проверить, загружено ли событие по ссылке.</summary>
        public bool IsLoaded(EventReference reference) => _eventsByGuid.ContainsKey(reference.Guid);
        /// <summary>Проверить, загружено ли событие по имени контейнера.</summary>
        public bool IsLoaded(string name) => _eventsByName.ContainsKey(name);

        // --- Lifecycle ---
        /// <summary>
        /// Создать и закэшировать контейнер события с указанным именем (если его ещё нет в кэше).
        /// </summary>
        public FMODEventContainer CreateInstance(string name, EventReference reference)
        {
            if (string.IsNullOrEmpty(name)) name = DeriveEventName(reference);
            if (_eventsByName.TryGetValue(name, out var existing)) return existing;

            var inst = RuntimeManager.CreateInstance(reference);
            if (!inst.isValid()) return null;

            var container = new FMODEventContainer(name, inst, reference);
            _eventContainers.Add(container);
            _eventsByName[name] = container;
            _eventsByGuid[reference.Guid] = container;
            Touch(container);
            EvictIfNeeded();
            return container;
        }
        /// <summary>
        /// Создать контейнер с указанным именем и режимом остановки по умолчанию.
        /// </summary>
        public FMODEventContainer CreateInstance(string name, EventReference reference, STOP_MODE stopMode)
        {
            if (string.IsNullOrEmpty(name)) name = DeriveEventName(reference);
            if (_eventsByName.TryGetValue(name, out var existing)) return existing;

            var inst = RuntimeManager.CreateInstance(reference);
            if (!inst.isValid()) return null;

            var container = new FMODEventContainer(name, inst, reference, stopMode);
            _eventContainers.Add(container);
            _eventsByName[name] = container;
            _eventsByGuid[reference.Guid] = container;
            Touch(container);
            EvictIfNeeded();
            return container;
        }
        /// <summary>Создать контейнер с авто-именем.</summary>
        public FMODEventContainer CreateInstance(EventReference reference) => CreateInstance(null, reference);
        /// <summary>Создать контейнер с авто-именем и кастомным режимом стопа.</summary>
        public FMODEventContainer CreateInstance(EventReference reference, STOP_MODE stopMode) => CreateInstance(null, reference, stopMode);
        /// <summary>Предзагрузить событие (создать контейнер без старта воспроизведения).</summary>
        public FMODEventContainer Preload(EventReference reference) => FindByRef(reference) ?? CreateInstance(reference);
        /// <summary>Гарантировать наличие контейнера в кэше и вернуть его.</summary>
        public FMODEventContainer EnsureLoaded(EventReference reference) => Preload(reference);

        /// <summary>
        /// Выгрузить и освободить контейнер по имени.
        /// </summary>
        public bool Unload(string name)
        {
            if (!_eventsByName.TryGetValue(name, out var c)) return false;
            _eventsByName.Remove(name);
            _eventsByGuid.Remove(c.EventReference.Guid);
            _eventContainers.Remove(c);
            _lastUsed.Remove(c);
            c.Dispose();
            return true;
        }
        /// <summary>
        /// Выгрузить и освободить контейнер по ссылке события.
        /// </summary>
        public bool Unload(EventReference reference)
        {
            if (!_eventsByGuid.TryGetValue(reference.Guid, out var c)) return false;
            _eventsByGuid.Remove(reference.Guid);
            _eventsByName.Remove(c.Name);
            _eventContainers.Remove(c);
            _lastUsed.Remove(c);
            c.Dispose();
            return true;
        }

        /// <summary>
        /// Остановить все известные контейнеры (включая музыку, если она находится в кэше событий).
        /// </summary>
        public void StopAll()
        {
            for (int i = 0; i < _eventContainers.Count; i++)
            {
                _eventContainers[i].EventInstance.getPlaybackState(out PLAYBACK_STATE state);
                if (state == PLAYBACK_STATE.PLAYING || state == PLAYBACK_STATE.STARTING)
                {
                    _eventContainers[i].Stop();
                }
            }
        }
        /// <summary>
        /// Установить паузу/снять паузу на всех управляемых контейнерах.
        /// </summary>
        public void SetPausedAll(bool paused)
        {
            for (int i = 0; i < _eventContainers.Count; i++)
            {
                _eventContainers[i].SetPaused(paused);
            }
        }

        public void Touch(FMODEventContainer c)
        {
            if (c != null) _lastUsed[c] = Time.time;
        }

        private void EvictIfNeeded()
        {
            if (_eventSettings == null || _eventSettings.MaxCachedEvents <= 0) return;
            if (_eventContainers.Count <= _eventSettings.MaxCachedEvents) return;

            FMODEventContainer candidate = null;
            float oldest = float.MaxValue;
            for (int i = 0; i < _eventContainers.Count; i++)
            {
                var c = _eventContainers[i];
                c.EventInstance.getPlaybackState(out PLAYBACK_STATE state);
                if (state == PLAYBACK_STATE.PLAYING || state == PLAYBACK_STATE.STARTING || state == PLAYBACK_STATE.STOPPING || state == PLAYBACK_STATE.SUSTAINING) continue;
                float last = _lastUsed.TryGetValue(c, out var t) ? t : 0f;
                if (last < oldest)
                {
                    oldest = last;
                    candidate = c;
                }
            }
            if (candidate == null) return;
            _eventContainers.Remove(candidate);
            _eventsByName.Remove(candidate.Name);
            _eventsByGuid.Remove(candidate.EventReference.Guid);
            _lastUsed.Remove(candidate);
            candidate.Dispose();
        }

        // replicate name derivation used by manager
        private static string DeriveEventName(EventReference reference)
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(reference.Path))
            {
                var parts = reference.Path.Split('/');
                return parts.Length > 0 ? parts[^1] : reference.Guid.ToString();
            }
#endif
            return reference.Guid.ToString();
        }
    }
}
