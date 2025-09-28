using System;
using System.Collections.Generic;
using UnityEngine;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// ScriptableObject-настройки сервиса шин (Bus).
    /// Содержит список шин для инициализации, параметры сохранения/загрузки громкостей и утилиты для ключей PlayerPrefs.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/FMOD/Services/Bus Service Asset", fileName = "FMODBusServiceAsset")]
    public class FMODBusServiceAsset : ScriptableObject
    {
        [Serializable]
        public class BusInit
        {
            /// <summary>
            /// Полный путь к шине FMOD (например, <c>bus:/</c>, <c>bus:/SFX</c>, <c>bus:/Music</c>).
            /// </summary>
            [Tooltip("FMOD bus path, e.g., bus:/, bus:/SFX, bus:/Music")] public string Path = "bus:/";
            /// <summary>
            /// Громкость по умолчанию, применяемая при инициализации.
            /// </summary>
            [Range(0f,1f)] public float DefaultVolume = 1f;
            /// <summary>
            /// Необязательный явный ключ PlayerPrefs для сохранения громкости. Если пусто — сгенерируется автоматически.
            /// </summary>
            [Tooltip("Override PlayerPrefs key for this bus volume. Leave empty to auto-generate.")]
            public string PlayerPrefsKey = string.Empty;
        }

        /// <summary>
        /// Список шин для инициализации менеджером при старте.
        /// </summary>
        [Header("Buses")]
        public List<BusInit> Buses = new();

        /// <summary>
        /// Сохранять ли громкость шин в PlayerPrefs.
        /// </summary>
        [Header("Persistence")] public bool PersistVolumes = true;
        /// <summary>
        /// Префикс, используемый для формирования ключей PlayerPrefs сохранения громкостей.
        /// </summary>
        [Tooltip("Prefix used for PlayerPrefs keys that store bus volumes")]
        public string VolumeKeyPrefix = "FMODBusVolume::";

        /// <summary>
        /// Найти конфигурацию инициализации шины по её пути.
        /// </summary>
        /// <param name="busPath">Путь к шине (например, <c>bus:/SFX</c>).</param>
        /// <returns>Экземпляр <see cref="BusInit"/> или null, если не найдено.</returns>
        public BusInit FindBusInit(string busPath)
        {
            if (string.IsNullOrEmpty(busPath) || Buses == null) return null;
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
            return $"{VolumeKeyPrefix}{init.Path}";
        }

        /// <summary>
        /// Построить рантайм-реализацию сервиса шин.
        /// </summary>
        /// <param name="manager">Ссылка на аудио-менеджер.</param>
        /// <param name="settingsAsset">Глобальные настройки аудиосистемы (могут быть null).</param>
        /// <returns>Экземпляр <see cref="IBusService"/>.</returns>
        public IBusService BuildRuntime(FMODAudioManager manager, FMODAudioSettingsAsset settingsAsset)
        {
            return new FmodBusService(manager, settingsAsset, this);
        }
    }
}
