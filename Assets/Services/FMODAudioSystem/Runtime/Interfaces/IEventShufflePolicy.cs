using System.Collections.Generic;
using FMODUnity;

namespace Services.FMODAudioSystem
{
    public interface IEventShufflePolicy
    {
        // Returns weights for the provided events. Must match list count.
        List<float> GetWeights(List<EventReference> events);
    }
}
