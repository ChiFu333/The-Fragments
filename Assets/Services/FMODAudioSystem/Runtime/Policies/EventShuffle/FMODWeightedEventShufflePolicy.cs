using System.Collections.Generic;
using FMODUnity;

namespace Services.FMODAudioSystem
{
    /// <summary>
    /// Runtime weighted shuffle policy that returns weights for given events using an internal registry.
    /// Missing events default to 0 weight.
    /// </summary>
    public sealed class FMODWeightedEventShufflePolicy : IEventShufflePolicy
    {
        private readonly Dictionary<System.Guid, float> _weightsByGuid;

        public FMODWeightedEventShufflePolicy(Dictionary<System.Guid, float> weightsByGuid)
        {
            _weightsByGuid = weightsByGuid ?? new Dictionary<System.Guid, float>();
        }

        public List<float> GetWeights(List<EventReference> events)
        {
            var result = new List<float>(events?.Count ?? 0);
            if (events == null) return result;
            for (int i = 0; i < events.Count; i++)
            {
                var guid = events[i].Guid;
                if (_weightsByGuid.TryGetValue(guid, out var w)) result.Add(w);
                else result.Add(0f);
            }
            return result;
        }
    }
}
