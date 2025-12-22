using UnityEngine;

public class TreeInteraction : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;
    private bool playerNearby = false;

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerNearby = true;
            dialogueManager.ShowPressEPrompt();
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerNearby = false;
            dialogueManager.HidePressEPrompt();
        }
    }

    private void Update()
    {
        // Press F to interact with tree
        if (playerNearby && Input.GetKeyDown(KeyCode.F))
        {
            dialogueManager.StartDialogue();
        }
    }
}