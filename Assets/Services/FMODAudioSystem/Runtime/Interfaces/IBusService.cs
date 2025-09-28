using Cysharp.Threading.Tasks;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Интерфейс сервиса управления шинами FMOD.
    /// Позволяет искать шину, управлять её громкостью, выполнять плавные изменения и дакинг,
    /// а также загружать/сохранять громкость из/в хранилище настроек (PlayerPrefs).
    /// </summary>
    public interface IBusService
    {
        /// <summary>Найти шину по пути (например, <c>bus:/Music</c>).</summary>
        FMODBus FindBus(string busPath);
        /// <summary>
        /// Установить громкость шины.
        /// </summary>
        /// <param name="busPath">Путь к шине.</param>
        /// <param name="volume">Значение 0..1.</param>
        /// <param name="persist">Сохранять ли значение (если включено в настройках сервиса).</param>
        void SetBusVolume(string busPath, float volume, bool persist);
        /// <summary>Плавно изменить громкость шины.</summary>
        UniTask FadeBusVolume(string busPath, float toVolume, float duration);
        /// <summary>Дакинг шины: атака, удержание, релиз.</summary>
        UniTask DuckBus(string busPath, float toVolume, float attack, float hold, float release);
        /// <summary>
        /// Загрузить сохранённую громкость для конфигурации шины.
        /// Возвращает -1, если сохранения нет.
        /// </summary>
        float LoadBusVolume(FMODBusServiceAsset.BusInit init);
    }
}
