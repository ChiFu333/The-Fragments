using System;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// ScriptableObject с настройками аудиосистемы FMOD. 
    /// Содержит параметры жизненного цикла, инициализацию шин (Bus) и список предзагружаемых событий.
    /// Также определяет расширенные опции, такие как лимит кэша событий и авто-остановка при смене сцены.
    /// </summary>
    [CreateAssetMenu(fileName = "FMODAudioSettings", menuName = "Audio/FMOD/Audio Settings", order = 10)]
    public class FMODAudioSettings : ScriptableObject
    {
        /// <summary>
        /// Делать ли объект менеджера неуничтожаемым при смене сцен.
        /// </summary>
        [Header("Lifecycle")] public new bool DontDestroyOnLoad = true;
        /// <summary>
        /// Время кроссфейда музыки по умолчанию (секунды).
        /// </summary>
        [Min(0f)] public float DefaultMusicFadeSeconds = 0.5f;

        /// <summary>
        /// Список шин (Bus), которые следует инициализировать при старте.
        /// </summary>
        [Header("Buses")] public List<BusInit> Buses = new();

        /// <summary>
        /// Список событий, которые следует предзагрузить при старте.
        /// </summary>
        [Header("Preload Events")] public List<PreloadEvent> PreloadEvents = new();

        [Header("Advanced")]
        [Tooltip("If > 0, the manager will keep at most this many cached EventInstances, evicting least-recently-used items.")]
        public int MaxCachedEvents = 0;
        [Tooltip("If true, StopAll(ALLOWFADEOUT) will be called on active scene changes.")]
        public bool StopAllOnSceneChange = false;

        [Serializable]
        public class BusInit
        {
            /// <summary>
            /// Полный путь к шине FMOD (например, bus:/, bus:/SFX, bus:/Music).
            /// </summary>
            [Tooltip("FMOD bus path, e.g., bus:/, bus:/SFX, bus:/Music")] public string Path = "bus:/";
            /// <summary>
            /// Громкость по умолчанию, применяемая при инициализации.
            /// </summary>
            [Range(0f,1f)] public float DefaultVolume = 1f;
            /// <summary>
            /// Необязательный пользовательский ключ PlayerPrefs для сохранения громкости этой шины.
            /// Если пусто — ключ будет сгенерирован автоматически.
            /// </summary>
            [Tooltip("Override PlayerPrefs key for this bus volume. Leave empty to auto-generate.")]
            public string PlayerPrefsKey = string.Empty;
        }

        [Serializable]
        public class PreloadEvent
        {
            /// <summary>
            /// Необязательное пользовательское имя. 
            /// Если пусто — будет использован последний сегмент пути или GUID события.
            /// </summary>
            [Tooltip("Optional custom name. If empty, the last path segment or GUID will be used.")]
            public string Name;
            /// <summary>
            /// Ссылка на событие FMOD.
            /// </summary>
            public EventReference Reference;
        }

        /// <summary>
        /// Найти конфигурацию инициализации шины по её пути.
        /// </summary>
        /// <param name="busPath">Путь к шине (например, bus:/SFX).</param>
        /// <returns>Экземпляр <see cref="BusInit"/> или null, если не найдено.</returns>
        public BusInit FindBusInit(string busPath)
        {
            if (string.IsNullOrEmpty(busPath)) return null;
            for (int i = 0; i < Buses.Count; i++)
            {
                if (string.Equals(Buses[i].Path, busPath, StringComparison.OrdinalIgnoreCase))
                    return Buses[i];
            }
            return null;
        }

        /// <summary>
        /// Получить ключ PlayerPrefs для сохранения громкости данной шины.
        /// </summary>
        /// <param name="init">Конфигурация шины.</param>
        /// <returns>Строковый ключ для PlayerPrefs.</returns>
        public string GetBusPrefsKey(BusInit init)
        {
            if (init == null) return string.Empty;
            if (!string.IsNullOrEmpty(init.PlayerPrefsKey)) return init.PlayerPrefsKey;
            // Default key pattern
            return $"FMODBusVolume::{init.Path}";
        }
    }
}
