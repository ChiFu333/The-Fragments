using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TagDialogueActor : EntityComponentDefinition
{
    public LocString actorName;
    public CMSEntityPfb voice;
    public List<Sprite> sprites;

    public Sprite GetSprite(DialogueActorSpriteType type)
    {
        return sprites[(int)type];
    }
}

public enum DialogueActorSpriteType
{
    Idle = 0,
    Speaking = 1,
}