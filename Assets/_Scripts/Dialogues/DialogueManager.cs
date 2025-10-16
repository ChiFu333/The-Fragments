using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public GameObject _nameText;
    private TMP_Text nameText;
    public GameObject _dialogueText;
    private TMP_Text dialogueText;

    public GameObject char1Pic;
    public GameObject char2Pic;

    private string[] _characters;

    private Image chr1;
    private Image chr2;
    private NPC_with_dialogue_AI.CharacterStates char1;
    private NPC_with_dialogue_AI.CharacterStates char2;

    public static bool isEnded = true;

    private Queue<string> sentences;

    void Start()
    {
        nameText = _nameText.GetComponent<TMP_Text>();
        dialogueText = _dialogueText.GetComponent<TMP_Text>();
        sentences = new Queue<string>();
        chr1 = char1Pic.GetComponent<Image>();
        chr2 = char2Pic.GetComponent<Image>();
    }

    public void StartDialogue(string[] current_sentences, string current_name, string[] _chars, NPC_with_dialogue_AI.CharacterStates _char1, NPC_with_dialogue_AI.CharacterStates _char2)
    {
        print("New Dialogue!");
        isEnded = false;
        _nameText.SetActive(true);
        _dialogueText.SetActive(true);
        nameText.text = current_name;
        _characters = _chars;
        char1 = _char1;
        char2 = _char2;
        sentences.Clear();

        foreach (string sentence in current_sentences)
        {
            sentences.Enqueue(sentence);
            Debug.Log(sentence);
        }

        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        CheckCharacter();
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }
        string sentence = sentences.Dequeue();
        StopAllCoroutines();
        StartCoroutine(TypeSentence(sentence));
    }

    IEnumerator TypeSentence(string sentence)
    {
        dialogueText.text = "";
        // Debug.Log(sentence);
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            // Debug.Log(letter);
            yield return new WaitForSeconds(0.05f);
        }
        Invoke("DisplayNextSentence", 1f);
        // if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) DisplayNextSentence();
    }

    public void CheckCharacter()
    {
        if (nameText.text == _characters[0])
        {
            chr1.sprite = char1.speak;
            chr2.sprite = char2.normal;
        }
        else if (nameText.text == _characters[1])
        {
            chr1.sprite = char1.normal;
            chr2.sprite = char2.speak;
        }
    }

    public void EndDialogue()
    {
        chr1.sprite = char1.normal;
        chr2.sprite = char2.normal;
        isEnded = true;
        _nameText.SetActive(true);
        _dialogueText.SetActive(true);
        Debug.Log("End of conversation.");
    }
}
