using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// ScriptableObject-представление плейлиста, из которого строится рантайм <see cref="FMODEventSequence"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/FMOD/Event Sequence (Playlist)", fileName = "NewFMODEventSequence")]
    public class FMODEventSequenceAsset : ScriptableObject
    {
        [Tooltip("Порядок треков для плейлиста")] public List<EventReference> Tracks = new List<EventReference>();
        [Tooltip("Зацикливать ли плейлист")] public bool Loop = true;
        [Tooltip("Длительность кроссфейда между треками, сек.")] public float CrossfadeSeconds = 1.0f;

        [Header("Advanced")]
        [Tooltip("Перемешивать ли порядок треков")] public bool Shuffle = false;
        [Tooltip("Окно 'не повторять' в кол-ве последних треков (0 = отключено)")] public int NoRepeatWindow = 0;
        [Tooltip("Задержка перед стартом трека, сек.")] public float PreDelaySeconds = 0f;
        [Tooltip("Задержка после окончания трека перед переходом к следующему, сек.")] public float PostDelaySeconds = 0f;

        public FMODEventSequence BuildRuntime()
        {
            var seq = new FMODEventSequence();
            if (Tracks != null) seq.Tracks.AddRange(Tracks);
            seq.Loop = Loop;
            seq.CrossfadeSeconds = Mathf.Max(0f, CrossfadeSeconds);
            seq.Shuffle = Shuffle;
            seq.NoRepeatWindow = Mathf.Max(0, NoRepeatWindow);
            seq.PreDelaySeconds = Mathf.Max(0f, PreDelaySeconds);
            seq.PostDelaySeconds = Mathf.Max(0f, PostDelaySeconds);
            return seq;
        }
    }
}
