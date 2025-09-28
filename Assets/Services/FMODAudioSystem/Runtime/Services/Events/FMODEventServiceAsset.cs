using System;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// ScriptableObject-настройки сервиса событий.
    /// Содержит список событий для предзагрузки при старте и параметры кэша контейнеров.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/FMOD/Services/Event Service Asset", fileName = "FMODEventServiceAsset")]
    public class FMODEventServiceAsset : ScriptableObject
    {
        [Serializable]
        public class PreloadEvent
        {
            /// <summary>
            /// Необязательное имя контейнера. Если пусто — будет использован последний сегмент пути или GUID события.
            /// </summary>
            [Tooltip("Optional custom name. If empty, the last path segment or GUID will be used.")]
            public string Name;
            /// <summary>
            /// Ссылка на FMOD-событие для предзагрузки.
            /// </summary>
            public EventReference Reference;
        }

        /// <summary>
        /// Список событий, которые будут предзагружены менеджером при старте.
        /// </summary>
        [Header("Preload Events")] public List<PreloadEvent> PreloadEvents = new();

        /// <summary>
        /// Максимальный размер кэша контейнеров событий. Если 0 — кэш не ограничен.
        /// При превышении лимита выполняется LRU-эвикция неиграющих контейнеров.
        /// </summary>
        [Header("Cache")] [Tooltip("If > 0, keep at most this many cached EventInstances, evicting least-recently-used items.")]
        public int MaxCachedEvents = 0;

        /// <summary>
        /// Построить рантайм-реализацию сервиса событий.
        /// </summary>
        /// <param name="settingsAsset">Глобальные настройки аудиосистемы (могут быть null).</param>
        /// <returns>Экземпляр <see cref="IEventService"/>.</returns>
        public IEventService BuildRuntime(FMODAudioSettingsAsset settingsAsset)
        {
            return new FmodEventService(settingsAsset, this);
        }
    }
}
