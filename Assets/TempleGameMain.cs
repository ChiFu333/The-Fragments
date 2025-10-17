using Cysharp.Threading.Tasks;
using UnityEngine;
using FMODUnity;

public class TempleGameMain : MonoBehaviour
{
    [Header("FMOD Event")]
    [SerializeField] private EventReference mySfxEvent;
    
    [HideInInspector] public Adv_Inga Inga;
    async void Start()
    {
        G.Adv_Main1 = this;
        Init();
        await UniTask.Delay(2000);
        //G.DialogueSystem.PlayDialogue(CMS.Get<CMSEntity>("CMS/DialogueSystem/Dialogues/SomeDialogue").Get<TagDialogue>()).Forget();
    }
    private void Init()
    {
        Inga = FindFirstObjectByType<Adv_Inga>();
        G.FMODAudioManager.SetBusVolume("bus:/Music", 0.06f, persist: false);
        G.FMODAudioManager.PlayOneShot(mySfxEvent, transform.position);
    }
}
