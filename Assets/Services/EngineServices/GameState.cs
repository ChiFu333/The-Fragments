using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Serialization;

public class GameState : MonoBehaviour, IService
{
    public int Points;

    private ConfigGameStates configGameStates;

    public void Init()
    {
        Reset();
        configGameStates = CMS.GetSingleComponent<ConfigGameStates>();
        if (configGameStates.overrideValues)
        {
            Points = configGameStates.points;
        }
    }

    public void Reset()
    {
        Points = 0;
    }
}