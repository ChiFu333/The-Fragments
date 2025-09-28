namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Сервис тегов событий FMOD. Позволяет:
    /// - регистрировать «шаблонные» теги для событий (по GUID),
    /// - автоматически привязывать теги к активным контейнерам при старте воспроизведения,
    /// - выполнять групповые операции по тегу (например, остановку всех событий с заданным тегом).
    /// </summary>
    public interface ITagService
    {
        /// <summary>
        /// Зарегистрировать тег-шаблон для события с указанным GUID. Тег будет автоматически применяться ко всем инстансам этого события.
        /// </summary>
        /// <param name="guid">GUID события из FMOD.</param>
        /// <param name="tag">Имя тега (без учёта регистра).</param>
        void RegisterTemplate(System.Guid guid, string tag);

        /// <summary>
        /// Удалить ранее зарегистрированный тег-шаблон для события.
        /// </summary>
        /// <param name="guid">GUID события FMOD.</param>
        /// <param name="tag">Имя тега.</param>
        void UnregisterTemplate(System.Guid guid, string tag);

        /// <summary>
        /// Привязать активные теги к только что запущенному контейнеру (вызывается менеджером при старте проигрывания).
        /// </summary>
        /// <param name="container">Контейнер события.</param>
        void BindActive(FMODEventContainer container);

        /// <summary>
        /// Снять активные теги с контейнера (вызывается при остановке/уничтожении контейнера).
        /// </summary>
        /// <param name="container">Контейнер события.</param>
        void UnbindActive(FMODEventContainer container);

        /// <summary>
        /// Остановить все активные контейнеры, помеченные указанным тегом.
        /// </summary>
        /// <param name="tag">Имя тега.</param>
        void StopByTag(string tag);
    }
}
