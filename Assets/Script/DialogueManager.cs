using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button skipButton;
    [SerializeField] private GameObject pressEPrompt;

    private List<string> dialogueLines = new List<string>
    {
        "Spirit Tree: \"Guardian… thank you for answering my call. Our forest is in danger.\"",
        "Spirit Tree: \"Goblins are cutting trees to steal the Heart Crystals hidden inside them.\"",
        "Spirit Tree: \"You must protect the forest. Stop the goblins and free the trapped animals.\"",
        "Spirit Tree: \"Collect Nature Orbs by defeating enemies and saving wildlife. Use them to restore parts of the forest.\"",
        "Spirit Tree: \"I believe in you, young guardian. Go, and keep the forest alive.\""
    };

    private int currentDialogueIndex = 0;
    private bool isDialogueActive = false;

    private void Start()
    {
        dialogueCanvas.gameObject.SetActive(false);
        pressEPrompt.gameObject.SetActive(false);
        skipButton.onClick.AddListener(SkipDialogue);
    }

    private void Update()
    {
        if (isDialogueActive && Input.GetMouseButtonDown(0))
        {
            NextDialogue();
        }
    }

    public void StartDialogue()
    {
        isDialogueActive = true;
        currentDialogueIndex = 0;
        dialogueCanvas.gameObject.SetActive(true);
        DisplayCurrentDialogue();
    }

    private void DisplayCurrentDialogue()
    {
        if (currentDialogueIndex < dialogueLines.Count)
        {
            dialogueText.text = dialogueLines[currentDialogueIndex];
        }
    }

    private void NextDialogue()
    {
        currentDialogueIndex++;

        if (currentDialogueIndex < dialogueLines.Count)
        {
            DisplayCurrentDialogue();
        }
        else
        {
            EndDialogue();
        }
    }

    private void SkipDialogue()
    {
        EndDialogue();
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        dialogueCanvas.gameObject.SetActive(false);
        currentDialogueIndex = 0;
    }

    public void ShowPressEPrompt()
    {
        pressEPrompt.gameObject.SetActive(true);
    }

    public void HidePressEPrompt()
    {
        pressEPrompt.gameObject.SetActive(false);
    }
}