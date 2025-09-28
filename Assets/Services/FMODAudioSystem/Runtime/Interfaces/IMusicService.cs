using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMODUnity;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Интерфейс музыкального сервиса: управление треками и плейлистами, кроссфейды и коллбеки.
    /// </summary>
    public interface IMusicService
    {
        /// <summary>
        /// Событие начала трека плейлиста. Передаёт ссылку события и индекс трека в плейлисте.
        /// </summary>
        event Action<EventReference, int> OnTrackStart;
        /// <summary>
        /// Событие завершения трека плейлиста. Передаёт ссылку события и индекс трека в плейлисте.
        /// </summary>
        event Action<EventReference, int> OnTrackEnd;

        /// <summary>
        /// Воспроизвести одиночный музыкальный трек с кроссфейдом.
        /// </summary>
        /// <param name="reference">FMOD-событие трека.</param>
        /// <param name="fadeSeconds">Длительность кроссфейда (если &lt; 0 — берётся из настроек).</param>
        void PlayMusic(EventReference reference, float fadeSeconds = -1f);
        /// <summary>
        /// Запустить плейлист из списка событий.
        /// </summary>
        void StartPlaylist(List<EventReference> playlist, bool loop = true, float crossfadeSeconds = -1f);
        /// <summary>
        /// Запустить плейлист по активу <see cref="FMODEventSequence"/>.
        /// </summary>
        void StartPlaylist(FMODEventSequence sequence, float crossfadeSeconds = -1f);
        /// <summary>Остановить текущий плейлист.</summary>
        void StopPlaylist();
        /// <summary>Перейти к следующему треку плейлиста.</summary>
        void NextTrack(float crossfadeSeconds = -1f);
        /// <summary>Перейти к предыдущему треку плейлиста.</summary>
        void PreviousTrack(float crossfadeSeconds = -1f);
    }
}
