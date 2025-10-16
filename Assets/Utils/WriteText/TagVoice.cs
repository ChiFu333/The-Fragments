using System;
using UnityEngine;
using TMPro;
[Serializable]
public class TagVoice : EntityComponentDefinition
{
    public AudioClip voice;
    public int deltaSound;
    public TMP_FontAsset font;
    public Material textMaterial;
    public Color color;
}
