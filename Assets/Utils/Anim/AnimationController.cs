using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Threading;
using UnityEngine.Serialization;

public class AnimationController : MonoBehaviour
{
    [HideInInspector] public SpriteRenderer targetRenderer;
    private TagAnimationData _currentAnimation;
    private CancellationTokenSource _cancellationTokenSource;

    private int frame = 0;

    private void Awake()
    {
        targetRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetAnimation(TagAnimationData newAnimation)
    {
        if (_currentAnimation != newAnimation)
        {
            StopAnimation();
            
            _currentAnimation = newAnimation;
            _cancellationTokenSource = new CancellationTokenSource();
            Anim(_currentAnimation, _cancellationTokenSource.Token).Forget();
        }
    }
    public async UniTask SetAnimOneShot(TagAnimationData newAnimation)
    {
        if (_currentAnimation != newAnimation)
        {
            StopAnimation();
            
            _currentAnimation = newAnimation;
            
            _cancellationTokenSource = new CancellationTokenSource();
            await PlayAnimOneShot(_currentAnimation, _cancellationTokenSource.Token);
        }
    }

    public void SetFlip(bool flip) => targetRenderer.flipX = flip;

    private async UniTask Anim(TagAnimationData myAnimData, CancellationToken cancellationToken)
    {
        frame = 0;
        transform.localPosition = myAnimData.animationOffset;
        while (_currentAnimation == myAnimData && !cancellationToken.IsCancellationRequested)
        {
            targetRenderer.sprite = myAnimData.frames[frame];
            frame = (frame + 1) % _currentAnimation.frames.Count;
            await UniTask.Delay((int)(1000 / myAnimData.framerate), DelayType.Realtime, cancellationToken: cancellationToken);
        }
    }
    private async UniTask PlayAnimOneShot(TagAnimationData myAnimData, CancellationToken cancellationToken)
    {
        frame = 0;
        bool firstZero = true;
        while (_currentAnimation == myAnimData && !cancellationToken.IsCancellationRequested)
        {
            targetRenderer.sprite = myAnimData.frames[frame];
            frame = (frame + 1) % _currentAnimation.frames.Count;
            await UniTask.Delay((int)(1000 / myAnimData.framerate), DelayType.Realtime, cancellationToken: cancellationToken);
            if (frame == 0 && !firstZero)
            {
                break;
            }
            firstZero = false;
        }
    }

    private void StopAnimation()
    {
        // Отменяем текущую задачу, если она существует
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    private void OnDestroy()
    {
        // Останавливаем анимацию при уничтожении объекта
        StopAnimation();
    }
}