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
            G.FMODAudioManager.Preload(_event);
            Debug.Log("Trying to play: " + _event + "");
            G.FMODAudioManager.Play(_event);
            Debug.Log("Done");
        }
    }
}