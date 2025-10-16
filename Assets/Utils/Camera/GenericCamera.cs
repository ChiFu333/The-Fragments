using System;
using UnityEngine;
using DG.Tweening;

public class GenericCamera : MonoBehaviour
{
    [Header("Настройки тряски")]
    [SerializeField] private float duration = 0.5f;     // Общая длительность
    [SerializeField] private float strength = 0.1f;     // Сила сдвига (меньше = микро-тряска)
    [SerializeField] private int vibrato = 50;          // Частота дрожания (высокая!)
    [SerializeField] private float randomness = 90f; 
    
    public Camera Camera { get; private set; }
    private Transform _visualTransform;
    private void Awake()
    {
        Camera = GetComponentInChildren<Camera>();
        _visualTransform = transform.GetChild(0);
    }
    private float timeToMove = 0.8f;
    public void MoveToGamePos(Vector3 pos) //это для резкого перемещения в точку, а не плавно следовать
    {
        transform.DOMove(new Vector3(pos.x, pos.y, transform.position.z), timeToMove).SetEase(Ease.OutBack, 0.75f);
    }
    public void Shake(float force = 1, float dur = 1) //Если и вызывать шейк, то очень круто добавить моушен блюр!
    {
        _visualTransform.DOKill();
        _visualTransform.localPosition = Vector3.zero;
        
        _visualTransform.DOShakePosition(
            duration * dur,
            strength * force,
            vibrato,
            randomness,
            fadeOut: true
        ).SetEase(Ease.OutQuad); // Линейное затухание для резкости
    }
}
