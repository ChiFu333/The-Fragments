using UnityEngine;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// ScriptableObject-настройки музыкального сервиса.
    /// Определяет политику перемешивания треков и значения по умолчанию для плейлистов.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/FMOD/Services/Music Service Asset", fileName = "FMODMusicServiceAsset")]
    public class FMODMusicServiceAsset : ScriptableObject
    {
        /// <summary>
        /// Актив-политика перемешивания треков. Определяет, как распределяются веса при выборе следующего трека.
        /// </summary>
        [Tooltip("Политика перемешивания событий")] public EventShufflePolicyAsset ShufflePolicyAsset;

        [Header("Defaults")]
        /// <summary>Кроссфейд по умолчанию (сек), если не задан явно.</summary>
        [Tooltip("Default crossfade for music if not specified elsewhere")] [Min(0f)]
        public float DefaultMusicFadeSeconds = 0.5f;
        /// <summary>Пауза до старта трека по умолчанию (сек).</summary>
        [Tooltip("Default pre-delay before a track starts if sequence doesn't specify")] [Min(0f)]
        public float DefaultPreDelaySeconds = 0f;
        /// <summary>Пауза после завершения трека по умолчанию (сек).</summary>
        [Tooltip("Default post-delay after a track ends if sequence doesn't specify")] [Min(0f)]
        public float DefaultPostDelaySeconds = 0f;
        /// <summary>Включать ли перемешивание по умолчанию.</summary>
        [Tooltip("Default shuffle mode if sequence doesn't specify")] public bool DefaultShuffle = false;
        /// <summary>Окно «не повторять последние N» по умолчанию.</summary>
        [Tooltip("Default no-repeat window if sequence doesn't specify")] [Min(0)] public int DefaultNoRepeatWindow = 0;

        /// <summary>
        /// Построить рантайм-реализацию музыкального сервиса.
        /// </summary>
        /// <param name="manager">Ссылка на аудио-менеджер.</param>
        /// <param name="settingsAsset">Глобальные настройки аудио (могут быть null).</param>
        /// <returns>Экземпляр <see cref="IMusicService"/>.</returns>
        public IMusicService BuildRuntime(FMODAudioManager manager, FMODAudioSettingsAsset settingsAsset)
        {
            IEventShufflePolicy policy = null;
            if (ShufflePolicyAsset != null) policy = ShufflePolicyAsset.BuildRuntime();
            return new FmodMusicService(manager, settingsAsset, this, policy);
        }
    }
}
