using System.Collections.Generic;
using FMODUnity;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Runtime-представление плейлиста FMOD событий.
    /// </summary>
    public class FMODEventSequence
    {
        public List<EventReference> Tracks = new List<EventReference>();
        public bool Loop = true;
        public float CrossfadeSeconds = 1.0f;
        public bool Shuffle = false;
        public int NoRepeatWindow = 0;
        public List<float> Weights = new List<float>();
        public float PreDelaySeconds = 0f;
        public float PostDelaySeconds = 0f;

        public FMODEventSequence() {}

        public FMODEventSequence(List<EventReference> tracks)
        {
            if (tracks != null) Tracks.AddRange(tracks);
        }
    }
}
