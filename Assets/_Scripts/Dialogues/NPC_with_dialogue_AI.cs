using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;

public class NPC_with_dialogue_AI : MonoBehaviour
{
    private int indexDialogue = 0;

    [System.Serializable]
    public struct Choices
    {
        public string first_choice;
        public string second_choice;
    }

    public List<Choices> myChoices = new List<Choices>();
    private bool isChoosing = false;


    public Dialogue dialogue;
    public TextAsset jsonData;
    private DialogueData _dialogueData;

    public GameObject choicesMenu;
    public TMP_Text first;
    public TMP_Text second;

    private DialogueManager dialogueMan;

    [System.Serializable]
    public struct CharacterStates
    {
        public Sprite normal;
        public Sprite speak;
    }

    public CharacterStates character_1;
    public CharacterStates character_2;

    // public GameObject char1Pic;
    // public GameObject char2Pic;

    // private Image chr1;
    // private Image chr2;


    private void Start()
    {
        _dialogueData = JsonUtility.FromJson<DialogueData>(jsonData.text);
        dialogueMan = FindFirstObjectByType<DialogueManager>();
        // chr1 = char1Pic.GetComponent<Image>();
        // chr2 = char2Pic.GetComponent<Image>();

        // print(_dialogueData.dialogueRoutes[0].name);
    }

    public void TriggerDialogue()
    {
        print(indexDialogue);
        dialogueMan.StartDialogue(_dialogueData.dialogueRoutes[indexDialogue].sentences, _dialogueData.dialogueRoutes[indexDialogue].name, _dialogueData.characters, character_1, character_2);
    }


    private void Update()
    {
        if (isChoosing)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                isChoosing = false;
                indexDialogue++;
                choicesMenu.SetActive(false);
                TriggerDialogue();
                indexDialogue += 2;
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                isChoosing = false;
                indexDialogue += 2;
                choicesMenu.SetActive(false);
                TriggerDialogue();
                indexDialogue++;
            }
        }
        if (DialogueManager.isEnded && !isChoosing && indexDialogue <= _dialogueData.dialogueRoutes.Length - 1)
        {
            PlayNextRoute();
        }

        // if (!DialogueManager.isEnded)
        // {
        //     // print(_dialogueData.dialogueRoutes[indexDialogue].name + " " + _dialogueData.characters[0] + " " + _dialogueData.characters[1]);
        //     if (_dialogueData.dialogueRoutes[indexDialogue - 1].name == _dialogueData.characters[0])
        //     {
        //         chr1.sprite = character_1.speak;
        //         chr2.sprite = character_2.normal;
        //     }
        //     else if (_dialogueData.dialogueRoutes[indexDialogue - 1].name == _dialogueData.characters[1])
        //     {
        //         chr1.sprite = character_1.normal;
        //         chr2.sprite = character_2.speak;
        //     }
        // }
        // else
        // {
        //     chr1.sprite = character_1.normal;
        //     chr2.sprite = character_2.normal;
        // }
    }


    private void PlayNextRoute()
    {
        print(_dialogueData.dialogueRoutes[indexDialogue].name);
        string _name = _dialogueData.dialogueRoutes[indexDialogue].name;
        if (_name != "_Choices_" && DialogueManager.isEnded)
        {
            TriggerDialogue();
            indexDialogue++;
        }
        else if (_name == "_Choices_" && DialogueManager.isEnded)
        {
            choicesMenu.SetActive(true);
            first.text = _dialogueData.dialogueRoutes[indexDialogue].sentences[0];
            second.text = _dialogueData.dialogueRoutes[indexDialogue].sentences[1];
            isChoosing = true;
        }
    }
}
