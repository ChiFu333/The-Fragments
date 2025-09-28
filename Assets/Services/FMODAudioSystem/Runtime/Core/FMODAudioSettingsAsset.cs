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
    public class FMODAudioSettingsAsset : ScriptableObject
    {
        /// <summary>
        /// Делать ли объект менеджера неуничтожаемым при смене сцен.
        /// </summary>
        [Header("Lifecycle")] public new bool DontDestroyOnLoad = true;
        /// <summary>
        /// Время кроссфейда музыки по умолчанию (секунды).
        /// </summary>
        [Min(0f)] public float DefaultMusicFadeSeconds = 0.5f;

        [Header("Advanced")]
        [Tooltip("If true, StopAll(ALLOWFADEOUT) will be called on active scene changes.")]
        public bool StopAllOnSceneChange = false;

        [Header("Services")]
        [Tooltip("Настройки музыкального сервиса и политика перемешивания.")]
        public FMODMusicServiceAsset MusicServiceAsset;
        [Tooltip("Настройки сервиса шин FMOD.")]
        public FMODBusServiceAsset BusServiceAsset;
        [Tooltip("Настройки сервиса событий FMOD.")]
        public FMODEventServiceAsset EventServiceAsset;
        [Tooltip("Настройки сервиса параметров FMOD.")]
        public FMODParameterServiceAsset ParameterServiceAsset;
        [Tooltip("Настройки сервиса банков FMOD.")]
        public FMODBankServiceAsset BankServiceAsset;
        [Tooltip("Настройки сервиса снапшотов FMOD.")]
        public FMODSnapshotServiceAsset SnapshotServiceAsset;
        [Tooltip("Настройки сервиса тегов FMOD.")]
        public FMODTagServiceAsset TagServiceAsset;

        
    }
}
