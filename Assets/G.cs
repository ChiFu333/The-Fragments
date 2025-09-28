using UnityEngine;
using Services.FMODAudioSystem;

public static class G 
{
    public static LocSystem LocSystem;
    public static AudioManager AudioManager;
    public static SceneLoader SceneLoader;
    public static GameState GameState;
    public static FMODAudioManager FmodFMODAudio;
    
    //Объекты в игре
    public static GameMain Main;
    public static TempleGameMain Adv_Main1;

}

[DefaultExecutionOrder(-9999)]
public static class GameBootstrapper
{
    private static bool _initialized = false;
    private static GameObject serviceHolder;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnBeforeSceneLoad()
    {
        if (_initialized) return;

        #if !UNITY_EDITOR
            CMS.Init();
        #endif
        R.InitAll();
        
        serviceHolder = new GameObject("===Services==="); 
        Object.DontDestroyOnLoad(serviceHolder);
        
        G.AudioManager = CreateSimpleService<AudioManager>();
        G.LocSystem = CreateSimpleService<LocSystem>();
        G.SceneLoader = CreateSimpleService<SceneLoader>();
        G.GameState = CreateSimpleService<GameState>();
        G.FmodFMODAudio = CreateSimpleService<FMODAudioManager>();
    }
    private static T CreateSimpleService<T>() where T : Component, IService
    {
        GameObject g = new GameObject(typeof(T).ToString());
        
        g.transform.parent = serviceHolder.transform;
        T t = g.AddComponent<T>();
        t.Init();
        return g.GetComponent<T>();
    }
}
public interface IService
{
    public void Init();
}
/*
 private GameObject CreateCanvas()
    {
        GameObject g = new GameObject("MainCanvas");
        Canvas c = g.AddComponent<Canvas>();

        CanvasScaler cs = g.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 1;

        g.AddComponent<GraphicRaycaster>();
        c.worldCamera = Camera.main;
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 100;
        return g;
    }
*/