using UnityEngine;
using FMOD.Studio;
using FMODUnity;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Обертка над шиной микшера FMOD (Bus), позволяющая конфигурировать и управлять ею из кода и инспектора.
    /// Предоставляет методы для установки громкости, мьюта, чтения громкости и остановки всех событий на шине.
    /// </summary>
    [System.Serializable]
    public class FMODBus
    {
        [SerializeField]
        [Tooltip("The full path to the FMOD Bus (e.g., bus:/SFX/Player)")]
        private string busPath = "bus:/"; // Дефолтный путь к мастер-шине

        private Bus busInstance;
        private bool isInitialized = false;

        /// <summary>
        /// Полный путь к шине (например, <c>bus:/</c>, <c>bus:/SFX</c>).
        /// </summary>
        public string Path => busPath;

        public FMODBus() { }
        public FMODBus(string path)
        {
            busPath = path;
        }

        /// <summary>
        /// Инициализировать ссылку на шину через <see cref="RuntimeManager.GetBus(string)"/>.
        /// </summary>
        /// <returns><c>true</c>, если инициализация прошла успешно.</returns>
        public bool Initialize()
        {
            if (isInitialized) return true;
            if (string.IsNullOrEmpty(busPath))
            {
                Debug.LogError("FMODBus: Bus path is empty. Cannot initialize.");
                return false;
            }
            try
            {
                busInstance = RuntimeManager.GetBus(busPath);
                isInitialized = busInstance.isValid();
                if (!isInitialized)
                {
                    Debug.LogWarning($"FMODBus: Could not find or initialize Bus at path: {busPath}.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"FMODBus: Error getting Bus '{busPath}'. Is FMOD Initialized? Exception: {e.Message}");
                isInitialized = false;
                return false;
            }
            return isInitialized;
        }

        /// <summary>
        /// Установить громкость шины (неотрицательное значение).
        /// </summary>
        /// <param name="volume">Желаемая громкость (0..∞). Будет клэмплена к минимуму 0.</param>
        public void SetVolume(float volume)
        {
            if (!EnsureInitialized()) return;
            float clampedVolume = Mathf.Max(0f, volume);
            var result = busInstance.setVolume(clampedVolume);
            LogIfError(result, "set volume");
        }

        /// <summary>
        /// Получить текущую громкость шины.
        /// </summary>
        /// <returns>Громкость (0..1+) или -1 при ошибке.</returns>
        public float GetVolume()
        {
            if (!EnsureInitialized()) return -1.0f;
            float volume;
            float finalVolume;
            var result = busInstance.getVolume(out volume, out finalVolume);
            if (LogIfError(result, "get volume")) return -1.0f;
            return volume;
        }

        /// <summary>
        /// Включить или выключить мьют шины.
        /// </summary>
        /// <param name="mute"><c>true</c>, чтобы заглушить шину.</param>
        public void SetMute(bool mute)
        {
            if (!EnsureInitialized()) return;
            var result = busInstance.setMute(mute);
            LogIfError(result, "set mute");
        }

        /// <summary>
        /// Проверить, находится ли шина в состоянии мьюта.
        /// </summary>
        /// <returns><c>true</c>, если шина заглушена.</returns>
        public bool IsMuted()
        {
            if (!EnsureInitialized()) return false;
            bool muted;
            var result = busInstance.getMute(out muted);
            if (LogIfError(result, "get mute")) return false;
            return muted;
        }

        /// <summary>
        /// Остановить все события, воспроизводимые через эту шину.
        /// </summary>
        /// <param name="stopMode">Режим остановки (мгновенно или с фейдаутом).</param>
        public void StopAllEvents(STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
        {
            if (!EnsureInitialized()) return;
            var result = busInstance.stopAllEvents(stopMode);
            LogIfError(result, "stop all events");
        }

        private bool EnsureInitialized()
        {
            if (!isInitialized)
            {
                Debug.LogWarning($"FMODBus '{busPath ?? "UNSET"}' is not initialized. Call Initialize() first.");
                return false;
            }
            if (!busInstance.isValid())
            {
                Debug.LogWarning($"FMODBus '{busPath}' reference is no longer valid. Needs re-initialization?");
                isInitialized = false;
                return false;
            }
            return true;
        }

        private bool LogIfError(FMOD.RESULT result, string operation)
        {
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogError($"FMODBus: Failed to {operation} on bus '{busPath}'. Error: {result}");
                return true;
            }
            return false;
        }
    }
}
