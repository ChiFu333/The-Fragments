using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Dialogue
{
    public string name;

    [System.Serializable]
    public struct dialogueRoute
    {
        [TextArea]
        public string[] Text;
    }

    public List<dialogueRoute> sentences = new List<dialogueRoute>();
}
