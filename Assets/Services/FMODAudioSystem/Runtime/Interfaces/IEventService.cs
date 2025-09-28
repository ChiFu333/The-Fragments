using FMOD.Studio;
using FMODUnity;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Интерфейс сервиса событий FMOD: создание/поиск контейнеров, управление кэшем и жизненным циклом.
    /// </summary>
    public interface IEventService
    {
        /// <summary>
        /// Создать и закэшировать контейнер события с указанным именем (если ещё не создан).
        /// </summary>
        FMODEventContainer CreateInstance(string name, EventReference sound);
        /// <summary>
        /// Создать контейнер с указанным именем и режимом остановки по умолчанию.
        /// </summary>
        FMODEventContainer CreateInstance(string name, EventReference sound, STOP_MODE stopMode);
        /// <summary>Создать контейнер с авто-именем.</summary>
        FMODEventContainer CreateInstance(EventReference sound);
        /// <summary>Создать контейнер с авто-именем и кастомным режимом стопа.</summary>
        FMODEventContainer CreateInstance(EventReference sound, STOP_MODE stopMode);
        /// <summary>Предзагрузить событие (создать контейнер без старта воспроизведения).</summary>
        FMODEventContainer Preload(EventReference reference);
        /// <summary>Гарантировать наличие контейнера и вернуть его.</summary>
        FMODEventContainer EnsureLoaded(EventReference reference);
        /// <summary>Выгрузить и освободить контейнер по имени.</summary>
        bool Unload(string name);
        /// <summary>Выгрузить и освободить контейнер по ссылке события.</summary>
        bool Unload(EventReference reference);
        /// <summary>Проверить, загружено ли событие по ссылке.</summary>
        bool IsLoaded(EventReference reference);
        /// <summary>Проверить, загружено ли событие по имени контейнера.</summary>
        bool IsLoaded(string name);

        /// <summary>Найти контейнер события по имени.</summary>
        FMODEventContainer FindByName(string name);
        /// <summary>Найти контейнер по экземпляру EventInstance.</summary>
        FMODEventContainer FindByInstance(EventInstance instance);
        /// <summary>Найти контейнер по ссылке события.</summary>
        FMODEventContainer FindByRef(EventReference reference);

        /// <summary>Остановить все управляемые контейнеры.</summary>
        void StopAll();
        /// <summary>Поставить на паузу/снять с паузы все управляемые контейнеры.</summary>
        void SetPausedAll(bool paused);
        /// <summary>Пометить контейнер как использованный (для стратегии эвикции LRU).</summary>
        void Touch(FMODEventContainer c);
    }
}
