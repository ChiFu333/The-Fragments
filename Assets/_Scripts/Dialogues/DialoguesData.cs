using System;
using System.Collections.Generic;


[Serializable]
public class DialogueData
{
    [Serializable]
    public class DialogueRoute
    {
        public string name;
        public string[] sentences;
    }

    public string[] characters;

    public DialogueRoute[] dialogueRoutes;
}

// [Serializable]
// public class NameData
// {
//     public string name;
// }

// [Serializable]
// public class SentencesData
// {
//     public string[] sentences;
//     // public TextData[] sentences;
// }

// [Serializable]
// public class TextData
// {
//     public string text;
// }