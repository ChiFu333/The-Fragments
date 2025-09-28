using UnityEngine;

namespace Services.FMODAudioSystem
{
    [CreateAssetMenu(menuName = "Audio/FMOD/Services/Snapshot Service Asset", fileName = "FMODSnapshotServiceAsset")]
    public class FMODSnapshotServiceAsset : ScriptableObject
    {
        public ISnapshotService BuildRuntime(FMODAudioManager manager)
        {
            return new FmodSnapshotService(manager);
        }
    }
}
