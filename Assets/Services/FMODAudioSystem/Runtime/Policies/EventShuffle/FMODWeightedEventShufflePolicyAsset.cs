using System.Collections.Generic;
using UnityEngine;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Актив-политика взвешенного перемешивания треков.
    /// Использует реестр весов событий (<see cref="FMODEventWeightsRegistryAsset"/>) и формирует словарь GUID→вес для рантайма.
    /// События, отсутствующие в реестре, получают вес 0.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/FMOD/Policies/Weighted Event Shuffle Policy", fileName = "FMODWeightedEventShufflePolicyAsset")]
    public class FMODWeightedEventShufflePolicyAsset : EventShufflePolicyAsset
    {
        /// <summary>
        /// Актив-реестр весов событий. Если не задан, все веса считаются нулевыми.
        /// </summary>
        public FMODEventWeightsRegistryAsset WeightsRegistry;

        /// <summary>
        /// Построить рантайм-реализацию политики (GUID→вес) для использования музыкальным сервисом.
        /// </summary>
        public override IEventShufflePolicy BuildRuntime()
        {
            var map = new Dictionary<System.Guid, float>();
            if (WeightsRegistry != null && WeightsRegistry.Entries != null)
            {
                foreach (var e in WeightsRegistry.Entries)
                {
                    map[e.Event.Guid] = Mathf.Max(0f, e.Weight);
                }
            }
            return new FMODWeightedEventShufflePolicy(map);
        }
    }
}
