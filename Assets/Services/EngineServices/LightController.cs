using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;

public class LightController : MonoBehaviour
{
    public Light2D globalLight;
    public void Init()
    {
        SetupLight();
    }
    public void SetupLight()
    {
        globalLight = FindObjectsByType<Light2D>(FindObjectsSortMode.None)
            .FirstOrDefault(l => l.lightType == Light2D.LightType.Global);
    }
    public async UniTask SetLight(float intensity)
    {
        await DOTween.To(
            () => globalLight.intensity,
            x => globalLight.intensity = x,
            1,//config.timeToChangeIntensity,                                  
            0.25f                                
        ).AsyncWaitForCompletion().AsUniTask();
    }
    public async UniTask RestoreLight()
    {
        await DOTween.To(
            () => globalLight.intensity, 
            x => globalLight.intensity = x,
            1,//config.timeToChangeIntensity,                                  
            0.25f                                
        ).AsyncWaitForCompletion().AsUniTask();
    }
    public async UniTask SetColor(Color color)
    {
        await DOTween.To(
            () => globalLight.color, 
            x => globalLight.color = x,
            color,                                  
            0.75f                                
        ).AsyncWaitForCompletion().AsUniTask();
    }
}
