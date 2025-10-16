using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private List<UIButtonScaling> buttons;
    private GameObject _LocCanvas;
    private GameObject _languagePanel;
    private List<LocString> buttonsText = new List<LocString>()
    {
        new LocString("Episodes", "Эпизоды"),
        new LocString("Continue", "Продолжить"),
        new LocString("Gallery", "Галерея"),
        new LocString("Settings", "Настройки"),
        new LocString("Authors", "Разработчики"),
        new LocString("Exit", "Выход"),
    };

    private void Awake()
    {
        Init();
        ShowLanguagePanel();
    }

    private void Init()
    {
        buttons = FindObjectsByType<UIButtonScaling>(FindObjectsSortMode.None)
        .Where(obj => obj.transform.parent.GetComponent<VerticalLayoutGroup>() != null)
        .OrderBy(b => b.transform.GetSiblingIndex())
        .ToList();

        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].transform.GetChild(0).gameObject.AddComponent<UITextSetter>().Init(buttonsText[i]);
            buttons[i].transform.GetChild(0).gameObject.GetComponent<UITextSetter>().UpdateUI();
        }

        buttons[0].OnClick.AddListener(() => G.SceneLoader.Load("Adventure_Loc1"));
    }

    private async UniTaskVoid ShowLanguagePanel()
    {
        if (_LocCanvas != null || !CMS.GetSingleComponent<ConfigMain>().showLocOnStart) return;
        await UniTask.Delay(700);
        _LocCanvas = new GameObject("LanguageCanvas");
        DontDestroyOnLoad(_LocCanvas);
        
        _LocCanvas.AddComponent<Canvas>();
        _LocCanvas.GetComponent<Canvas>().worldCamera = Camera.main;
        _LocCanvas.GetComponent<Canvas>().sortingOrder = 1001; 
        _LocCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
        
        _LocCanvas.AddComponent<GraphicRaycaster>();
        
        CanvasScaler cs = _LocCanvas.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 1;
        if(CMS.GetSingleComponent<ConfigMain>().showLocOnStart)
            _languagePanel = Instantiate(Resources.Load<GameObject>("Services/" + "LanguageSelector"), _LocCanvas.transform, false);
        
    }
    
}
