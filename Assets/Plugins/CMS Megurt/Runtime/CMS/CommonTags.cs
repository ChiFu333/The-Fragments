using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TagSprite : EntityComponentDefinition
{
    public Sprite sprite;
}
[Serializable]
public class TagAnimationData : EntityComponentDefinition {
    [field: SerializeField] public float framerate { get; private set; }
    [field: SerializeField] public Vector2 animationOffset { get; private set; }
    [field: SerializeField] public List<Sprite> frames { get; private set; } = new List<Sprite>();
}