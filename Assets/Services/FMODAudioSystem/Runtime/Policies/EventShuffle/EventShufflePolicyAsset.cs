using UnityEngine;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Базовый тип ScriptableObject для политик перемешивания музыкальных событий.
    /// Используется для строгой типизации полей инспектора (например, в <see cref="FMODMusicServiceAsset"/>),
    /// чтобы в ObjectField можно было выбрать только корректные активы-политики.
    /// </summary>
    public abstract class EventShufflePolicyAsset : ScriptableObject, IEventShufflePolicyAsset
    {
        /// <summary>
        /// Построить рантайм-реализацию политики перемешивания, которая будет использоваться музыкальным сервисом.
        /// </summary>
        /// <returns>Экземпляр <see cref="IEventShufflePolicy"/> для рантайма.</returns>
        public abstract IEventShufflePolicy BuildRuntime();
    }
}
