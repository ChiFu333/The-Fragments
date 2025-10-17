using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TagDialogue : EntityComponentDefinition
{
    public CMSEntityPfb LeftActor;
    public CMSEntityPfb RightActor;

    public List<DialogueLine> scenario;
}

[Serializable]
public class DialogueLine
{
    public bool isLeftActorSpeak;
    public DialogueActorSpriteType spriteType;
    public LocString textLine;
    
}
