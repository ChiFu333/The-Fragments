using UnityEngine;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Минимальный загрузчик менеджера аудио FMOD, создающий <see cref="FMODAudioManager"/> при старте,
    /// если он отсутствует в сцене. Может быть отключен при использовании сервисной системы.
    /// </summary>
    [DefaultExecutionOrder(-9000)]
    public static class FMODAudioBootstrapper
    {
        /// <summary>
        /// Вызывается перед загрузкой первой сцены. Гарантирует существование <see cref="FMODAudioManager"/>.
        /// Если настройки находятся в Resources, применяет их.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureAudioManager()
        {
            // If an AudioManager already exists, do nothing
            if (Object.FindObjectsByType<FMODAudioManager>(FindObjectsSortMode.None).Length <= 0) return;

            // Try to find settings in Resources
            var settings = Resources.Load<FMODAudioSettingsAsset>("Audio/FMOD/FMODAudioSettings");

            var go = new GameObject("FMOD AudioManager");
            var mgr = go.AddComponent<FMODAudioManager>();
            if (settings != null) mgr.Configure(settings);
        }
    }
}
