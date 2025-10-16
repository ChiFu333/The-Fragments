using UnityEngine;

public class TempleGameMain : MonoBehaviour
{
    [HideInInspector] public Adv_Inga Inga;
    void Start()
    {
        G.Adv_Main1 = this;
        Init();
    }
    private void Init()
    {
        Inga = FindFirstObjectByType<Adv_Inga>();
        _ = FindFirstObjectByType<TextThrower>().ThrowText(
            new LocString("", "Привет, это тестовый текст!"),
            CMS.Get<CMSEntity>("CMS/Voices/TempleVoice").Get<TagVoice>());
    }
}
