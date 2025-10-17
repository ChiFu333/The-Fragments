using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueSystem : MonoBehaviour, IService, INeedCameraForCanvas
{
    [HideInInspector] public DialogueCanvas DialogueCanvas;
    public void Init()
    {
        GameObject g = Instantiate(Resources.Load<GameObject>("Services/" + "DialogueSystem"), GameBootstrapper.serviceHolder.transform, true);
        DialogueCanvas = g.GetComponent<DialogueCanvas>();
    }
    public async UniTask PlayDialogue(TagDialogue dialogue)
    {
        await DialogueCanvas.PlayDialogue(dialogue);
    }

    public void UpdateCanvasField(Camera c)
    {
        DialogueCanvas.Canvas.worldCamera = c;
    }
}
