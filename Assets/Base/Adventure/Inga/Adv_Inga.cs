using System;
using UnityEngine;

public class Adv_Inga : MonoBehaviour
{
    [SerializeField] private float speed;

    [HideInInspector] public SpriteRenderer visual;
    [HideInInspector] public Rigidbody2D body;
    [HideInInspector] public AnimationController animController;

    private float _inputX;
    private TagAnimationData _standAnim, _walkAnim;
    void Awake()
    {
        visual = GetComponentInChildren<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        animController = GetComponentInChildren<AnimationController>();

        _standAnim = CMS.Get<CMSEntity>("CMS/Animations/IngaStanding").Get<TagAnimationData>();
        _walkAnim = CMS.Get<CMSEntity>("CMS/Animations/IngaWalking").Get<TagAnimationData>();
    }

    private void Start()
    {
        animController.SetAnimation(_standAnim);
    }

    void Update()
    {
        _inputX = Input.GetAxisRaw("Horizontal");
        
        animController.SetAnimation(_inputX == 0 ? _standAnim : _walkAnim);
        animController.SetFlip(_inputX < 0);
    }
    private void FixedUpdate()
    {
        body.linearVelocityX = _inputX * speed;
    }
}
