using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace Services.FMODAudioSystem
{
    public class SimpleMusic : MonoBehaviour
    {
        [SerializeField] private EventReference _event;
        
        private FMODEventContainer _instance;
        
        private void Start()
        {
            G.FmodFMODAudio.Preload(_event);
            Debug.Log("Trying to play: " + _event + "");
            G.FmodFMODAudio.Play(_event);
            Debug.Log("Done");
        }
    }
}