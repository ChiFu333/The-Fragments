using DG.Tweening;
using UnityEngine;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// ScriptableObject-настройки сервиса параметров FMOD.
    /// Определяет поведение плавных изменений (твинов) для глобальных и локальных параметров.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/FMOD/Services/Parameter Service Asset", fileName = "FMODParameterServiceAsset")]
    public class FMODParameterServiceAsset : ScriptableObject
    {
        /// <summary>
        /// Тип кривой (Ease) для плавного изменения глобальных параметров по умолчанию.
        /// </summary>
        [Header("Ramps Defaults")]
        public Ease GlobalParameterEase = Ease.Linear;
        /// <summary>
        /// Тип кривой (Ease) для плавного изменения параметров конкретных событий по умолчанию.
        /// </summary>
        public Ease EventParameterEase = Ease.Linear;
        /// <summary>
        /// Использовать ли неускоренное время Unity (unscaledTime) при обновлении твинов параметров.
        /// </summary>
        [Tooltip("Use unscaled time for DOTween updates in parameter ramps")]
        public bool UseUnscaledTime = false;

        /// <summary>
        /// Построить рантайм-реализацию сервиса параметров.
        /// </summary>
        /// <param name="manager">Ссылка на аудио-менеджер.</param>
        /// <returns>Экземпляр <see cref="IParameterService"/>.</returns>
        public IParameterService BuildRuntime(FMODAudioManager manager)
        {
            return new FmodParameterService(manager, this);
        }
    }
}
