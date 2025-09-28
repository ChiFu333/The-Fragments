using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Интерфейс сервиса снапшотов FMOD. Снапшоты представляют собой события-состояния микса,
    /// которые запускаются/останавливаются поверх текущего микса. Сервис также предоставляет стек приоритетов.
    /// </summary>
    public interface ISnapshotService
    {
        /// <summary>
        /// Запустить снапшот и вернуть соответствующий контейнер события.
        /// </summary>
        /// <param name="snapshot">Ссылка на снапшот.</param>
        /// <returns>Контейнер события снапшота, либо null, если не удалось создать.</returns>
        FMODEventContainer StartSnapshot(EventReference snapshot);
        /// <summary>
        /// Остановить запущенный снапшот.
        /// </summary>
        /// <param name="snapshot">Ссылка на снапшот.</param>
        /// <param name="stopMode">Режим остановки (ALLOWFADEOUT/IMMEDIATE).</param>
        void StopSnapshot(EventReference snapshot, STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT);
        /// <summary>
        /// Положить снапшот на стек приоритетов. Если какой-то уже активен, он будет остановлен перед запуском нового.
        /// </summary>
        /// <param name="snapshot">Ссылка на снапшот.</param>
        /// <param name="fadeSeconds">Опциональная задержка между остановкой предыдущего и запуском нового.</param>
        UniTask PushSnapshotAsync(EventReference snapshot, float fadeSeconds = 0.25f);
        /// <summary>
        /// Снять снапшот со стека и восстановить предыдущий (если был). Опциональная задержка для плавности.
        /// </summary>
        /// <param name="fadeSeconds">Опциональная задержка перед возвратом предыдущего снапшота.</param>
        UniTask PopSnapshotAsync(float fadeSeconds = 0.25f);
    }
}
