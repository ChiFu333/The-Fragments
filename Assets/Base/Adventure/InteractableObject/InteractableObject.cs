using System;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class InteractableObject : MonoBehaviour
{
    [Header("Событие")]
    public UnityEvent OnInteract;

    [Header("Подсказка")]
    public GameObject hintPanel;
    public Vector3 hintOffset = new Vector3(0, 2f, 0); 
    

    private bool isHintVisible = false;


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ShowHint();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            HideHint();
        }
    }

    public void Interact()
    {
        OnInteract?.Invoke();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    public void ShowHint()
    {
        if (hintPanel != null)
        {
            hintPanel.SetActive(true);
            isHintVisible = true;

            hintPanel.transform.position = transform.position + hintOffset;
        }
    }

    public void HideHint()
    {
        if (hintPanel != null)
        {
            hintPanel.SetActive(false);
            isHintVisible = false;
        }
    }

    public void PrintSomething(string s)
    {
        print(s);
    }
}