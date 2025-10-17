using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;

public class SceneLoader : MonoBehaviour, IService
{
    public string currentSceneName = null;
    
    private GameObject _fadeCanvas;
    private bool showFade;
    
    public void Init()
    {
        if(CMS.GetSingleComponent<ConfigMain>().showFading)
            CreateFadeCanvas();
        currentSceneName = SceneManager.GetActiveScene().name;
    }

    public void StartFirstScene()
    {
        SetupCameraField();
        _ = Unfade(0.75f);
    }
    public async UniTask Load(string n, float fadeSpeed = 0.75f)
    {
        await Fade(fadeSpeed);
        await LoadScene(n);
        SetupCameraField();
        await Unfade(fadeSpeed);
        //G.PausePanel.Init();
        //if(n == "MainMenu") G.AudioManager.PlayMusic(R.Audio.mainMenuMusic);
    }
    private async UniTask LoadScene(string n)
    {
        if(currentSceneName == null) return;
        await SceneManager.LoadSceneAsync(n).ToUniTask();
        await UniTask.Yield();
        currentSceneName = n;
    }
    private void CreateFadeCanvas()
    {
        _fadeCanvas = new GameObject("Canvas - FadeCanvas");
        DontDestroyOnLoad(_fadeCanvas);
        _fadeCanvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
        _fadeCanvas.GetComponent<Canvas>().sortingOrder = 9999; 

        _fadeCanvas.AddComponent<GraphicRaycaster>();

        GameObject fadeImage = new GameObject("FadeImage");
        fadeImage.transform.parent = _fadeCanvas.transform;
        
        fadeImage.AddComponent<Image>().color = Color.white;
        fadeImage.GetComponent<Image>().material = new Material(R.materialFade);
        
        fadeImage.GetComponent<RectTransform>().anchorMin = Vector2.zero; // Якоря в нижний левый угол
        fadeImage.GetComponent<RectTransform>().anchorMax = Vector2.one;  // Якоря в верхний правый угол
        fadeImage.GetComponent<RectTransform>().offsetMin = Vector2.zero; // Нулевые отступы
        fadeImage.GetComponent<RectTransform>().offsetMax = Vector2.zero; 
    }

    private void SetupCameraField()
    {
        Camera c = FindFirstObjectByType<Camera>();
        G.Services.OfType<INeedCameraForCanvas>().ToList().ForEach(x => x.UpdateCanvasField(c));
    }
    private async UniTask Fade(float duration)
    {
        if (_fadeCanvas == null)
        {
            await UniTask.Yield();
            return;
        }

        _fadeCanvas.GetComponentInChildren<Image>().raycastTarget = true;
        await _fadeCanvas.transform.GetChild(0).GetComponent<Image>().material
            .DoChangeMaterialFloat("_FadeAmount", -0.05f, duration);
    }

    private async UniTask Unfade(float duration)
    {
        if (_fadeCanvas == null)
        {
            await UniTask.Yield();
            return;
        }
        await _fadeCanvas.transform.GetChild(0).GetComponent<Image>().material
                .DoChangeMaterialFloat("_FadeAmount", 1, duration);
        _fadeCanvas.GetComponentInChildren<Image>().raycastTarget = false;
    }
}

