using System;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Реестр весов событий для взвешенного перемешивания плейлистов.
    /// Формирует таблицу соответствий EventReference → вес (float).
    /// Используется активом <see cref="FMODWeightedEventShufflePolicyAsset"/> при построении рантайм-политики.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/FMOD/Event Weights Registry", fileName = "FMODEventWeightsRegistry")]
    public class FMODEventWeightsRegistryAsset : ScriptableObject
    {
        /// <summary>
        /// Запись реестра: событие и его вес. Отрицательные значения будут обрезаны до 0 при построении политики.
        /// </summary>
        [Serializable]
        public struct Entry
        {
            /// <summary>Ссылка на FMOD-событие.</summary>
            public EventReference Event;
            /// <summary>Вес события при выборе (чем больше, тем выше вероятность).</summary>
            public float Weight;
        }

        /// <summary>
        /// Список записей реестра (порядок не важен).
        /// </summary>
        public List<Entry> Entries = new List<Entry>();
    }
}
