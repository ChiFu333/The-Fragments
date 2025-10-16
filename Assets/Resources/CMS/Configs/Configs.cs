using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

[Serializable]
public class ConfigMain : EntityComponentDefinition
{
    [Header("Fade settings")] 
    public bool showFading = true;
    [Header("Localization settings")] 
    public bool showLocOnStart = true;
    public bool rewriteLocWithRus = false;
}

[Serializable]
public class ConfigGameStates : EntityComponentDefinition
{
    public bool overrideValues;
    public int points;
}
