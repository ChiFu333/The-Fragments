using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueCanvas : MonoBehaviour
{
    [HideInInspector] public Canvas Canvas;
    [SerializeField] public Image fadeBack;
    [SerializeField] public Image leftActorIm;
    [SerializeField] public Image rightActorIm;
    [SerializeField] public GameObject nameLabelHolder;
    [SerializeField] public TextThrower textAnimator;

    private float _inPos = 320f;
    private float _outPos = 1500;
    private float _timeToShow = 0.5f;
    private float _timeToHide = 0.45f;

    private float _timeToChangeScale = 0.5f;
    private float _bigScale = 1.15f;
    private float _smallScale = 0.85f;
    private float _inBigPos = 377f;
    private float _inSmallPos = 285f;

    private void Awake()
    {
        Canvas = GetComponent<Canvas>();
        leftActorIm.material = new Material(leftActorIm.material);
        rightActorIm.material = new Material(rightActorIm.material);
    }
    public async UniTask PlayDialogue(TagDialogue dialogue)
    {
        fadeBack.raycastTarget = true;
        textAnimator.CleanText();
        if (dialogue.scenario[0].isLeftActorSpeak)
        {
            nameLabelHolder.GetComponentInChildren<TMP_Text>().text =
                dialogue.LeftActor.AsEntity().Get<TagDialogueActor>().actorName.ToString();
        }
        else
        {
            nameLabelHolder.GetComponentInChildren<TMP_Text>().text =
                dialogue.RightActor.AsEntity().Get<TagDialogueActor>().actorName.ToString();
        }

        leftActorIm.sprite = dialogue.LeftActor.AsEntity().Get<TagDialogueActor>().GetSprite(DialogueActorSpriteType.Idle);
        leftActorIm.GetComponent<RectTransform>().anchoredPosition = new Vector2(-2000, 0);
        rightActorIm.sprite = dialogue.RightActor.AsEntity().Get<TagDialogueActor>().GetSprite(DialogueActorSpriteType.Idle);
        rightActorIm.GetComponent<RectTransform>().anchoredPosition = new Vector2(2000, 0);
        await Setup(true);
        bool currentActorBool = dialogue.scenario[0].isLeftActorSpeak;
        for (int i = 0; i < dialogue.scenario.Count; i++)
        {
            if (dialogue.scenario[i].isLeftActorSpeak)
            {
                nameLabelHolder.GetComponentInChildren<TMP_Text>().text = dialogue.LeftActor.AsEntity().Get<TagDialogueActor>().actorName.ToString();
                leftActorIm.sprite = dialogue.LeftActor.AsEntity().Get<TagDialogueActor>()
                    .GetSprite(dialogue.scenario[i].spriteType);
                rightActorIm.sprite = dialogue.RightActor.AsEntity().Get<TagDialogueActor>()
                    .GetSprite(DialogueActorSpriteType.Idle);
            }
            else
            {
                nameLabelHolder.GetComponentInChildren<TMP_Text>().text = dialogue.RightActor.AsEntity().Get<TagDialogueActor>().actorName.ToString();
                leftActorIm.sprite = dialogue.LeftActor.AsEntity().Get<TagDialogueActor>()
                    .GetSprite(DialogueActorSpriteType.Idle);
                rightActorIm.sprite = dialogue.RightActor.AsEntity().Get<TagDialogueActor>()
                    .GetSprite(dialogue.scenario[i].spriteType);
            }
            
            if (currentActorBool != dialogue.scenario[i].isLeftActorSpeak || i == 0)
            {
                currentActorBool = dialogue.scenario[i].isLeftActorSpeak;
                textAnimator.CleanText();
                await ScaleActor(dialogue.scenario[i].isLeftActorSpeak);
            }
            await textAnimator.ThrowText(dialogue.scenario[i].textLine, 
                (dialogue.scenario[i].isLeftActorSpeak ? dialogue.LeftActor : dialogue.RightActor)
                .AsEntity().Get<TagDialogueActor>().voice.AsEntity().Get<TagVoice>());
            await UniTask.Delay(1000);
        }
        await UniTask.Delay(1000);
        await ScaleActorNormal();
        await Setup(false);
        fadeBack.raycastTarget = true; //750
    }
    public async UniTask Setup(bool toSetup)
    {
        if (toSetup)
        {
            await fadeBack.DOFade(0.75f, _timeToShow).AsyncWaitForCompletion();
            leftActorIm.GetComponent<RectTransform>().localScale = Vector3.one;
            rightActorIm.GetComponent<RectTransform>().localScale = Vector3.one;
            _ = leftActorIm.GetComponent<RectTransform>().DOAnchorPos(new Vector2(-_inPos, 0), _timeToShow)
                .SetEase(Ease.OutCubic).AsyncWaitForCompletion();
            await UniTask.Delay(350);
            await rightActorIm.GetComponent<RectTransform>().DOAnchorPos(new Vector2(_inPos, 0), _timeToShow)
                .SetEase(Ease.OutCubic).AsyncWaitForCompletion();
            textAnimator.CleanText();
            textAnimator.transform.DOSScaleSaveBounce(Vector3.one, _timeToShow).Forget();
            await nameLabelHolder.transform.DOSScaleSaveBounce(Vector3.one, _timeToShow);
        }
        else
        {
            _ = leftActorIm.GetComponent<RectTransform>().DOAnchorPos(new Vector2(-_outPos, 0), _timeToHide)
                .SetEase(Ease.InCubic).AsyncWaitForCompletion();
            nameLabelHolder.transform.DOSScaleSaveBounce(Vector3.zero, _timeToHide).Forget();
            textAnimator.transform.DOSScaleSaveBounce(Vector3.zero, _timeToShow).Forget();
            await UniTask.Delay((int)(_timeToHide * 1000) - 100);
            await rightActorIm.GetComponent<RectTransform>().DOAnchorPos(new Vector2(_outPos, 0), _timeToHide)
                .SetEase(Ease.InCubic).AsyncWaitForCompletion();
            leftActorIm.GetComponent<RectTransform>().localScale = Vector3.zero;
            rightActorIm.GetComponent<RectTransform>().localScale = Vector3.zero;
            await fadeBack.DOFade(0f, _timeToHide).AsyncWaitForCompletion();
        }
    }

    public async UniTask ScaleActor(bool isLeftActor)
    {
        if (isLeftActor)
        {
            leftActorIm.material.DoChangeMaterialFloat("_GreyscaleBlend", 0, _timeToChangeScale).Forget();
            leftActorIm.GetComponent<RectTransform>().DOAnchorPos(new Vector2(-_inBigPos, 0), _timeToChangeScale).SetEase(Ease.OutCubic);
            leftActorIm.GetComponent<RectTransform>().DOScale(Vector3.one * _bigScale, _timeToChangeScale).SetEase(Ease.OutCubic);
            rightActorIm.material.DoChangeMaterialFloat("_GreyscaleBlend", 1, _timeToChangeScale).Forget();
            rightActorIm.GetComponent<RectTransform>().DOAnchorPos(new Vector2(_inSmallPos, 0), _timeToChangeScale).SetEase(Ease.OutCubic);
            rightActorIm.GetComponent<RectTransform>().DOScale(Vector3.one * _smallScale, _timeToChangeScale).SetEase(Ease.OutCubic);
            
            await UniTask.Delay((int)(_timeToChangeScale * 1000));
        }
        else
        {
            rightActorIm.material.DoChangeMaterialFloat("_GreyscaleBlend", 0, _timeToChangeScale).Forget();
            rightActorIm.GetComponent<RectTransform>().DOAnchorPos(new Vector2(_inBigPos, 0), _timeToChangeScale).SetEase(Ease.OutCubic);
            rightActorIm.GetComponent<RectTransform>().DOScale(Vector3.one * _bigScale, _timeToChangeScale).SetEase(Ease.OutCubic);
            leftActorIm.material.DoChangeMaterialFloat("_GreyscaleBlend", 1, _timeToChangeScale).Forget();
            leftActorIm.GetComponent<RectTransform>().DOAnchorPos(new Vector2(-_inSmallPos, 0), _timeToChangeScale).SetEase(Ease.OutCubic);
            leftActorIm.GetComponent<RectTransform>().DOScale(Vector3.one * _smallScale, _timeToChangeScale).SetEase(Ease.OutCubic);
            await UniTask.Delay((int)(_timeToChangeScale * 1000));
        }
    }

    private async UniTask ScaleActorNormal()
    {
        leftActorIm.material.DoChangeMaterialFloat("_GreyscaleBlend", 0, _timeToChangeScale).Forget();
        rightActorIm.material.DoChangeMaterialFloat("_GreyscaleBlend", 0, _timeToChangeScale).Forget();
        leftActorIm.GetComponent<RectTransform>().DOAnchorPos(new Vector2(-_inPos, 0), _timeToChangeScale).SetEase(Ease.OutCubic);
        leftActorIm.GetComponent<RectTransform>().DOScale(Vector3.one, _timeToChangeScale).SetEase(Ease.OutCubic);
        rightActorIm.GetComponent<RectTransform>().DOAnchorPos(new Vector2(_inPos, 0), _timeToChangeScale).SetEase(Ease.OutCubic);
        rightActorIm.GetComponent<RectTransform>().DOScale(Vector3.one, _timeToChangeScale).SetEase(Ease.OutCubic);
        await UniTask.Delay(700);
    }
}
