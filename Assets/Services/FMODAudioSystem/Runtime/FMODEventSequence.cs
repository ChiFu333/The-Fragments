using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Плейлист FMOD-событий как ScriptableObject-актив. Можно создавать несколько разных плейлистов
    /// и запускать их через менеджер аудио.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/FMOD/Event Sequence (Playlist)", fileName = "NewFMODEventSequence")]
    public class FMODEventSequence : ScriptableObject
    {
        /// <summary>Список треков (FMOD EventReference) в порядке воспроизведения.</summary>
        public List<EventReference> Tracks = new();
        /// <summary>Зацикливать последовательность.</summary>
        public bool Loop = true;
        /// <summary>Кроссфейд между треками при переключении.</summary>
        [Min(0f)] public float CrossfadeSeconds = 0.5f;
    }
}
