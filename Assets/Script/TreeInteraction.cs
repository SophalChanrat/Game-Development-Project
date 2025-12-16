using UnityEngine;

public class TreeInteraction : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;
    private bool playerNearby = false;

    private void Start()
    {
        Debug.Log("TreeInteraction script started");
    }

    private void OnTriggerEnter(Collider collision)
    {
        Debug.Log("Collider touched: " + collision.gameObject.name + " with tag: " + collision.CompareTag("Player"));

        if (collision.CompareTag("Player"))
        {
            playerNearby = true;
            Debug.Log("PLAYER DETECTED!");
            if (dialogueManager != null)
            {
                dialogueManager.ShowPressEPrompt();
                Debug.Log("ShowPressEPrompt called");
            }
            else
            {
                Debug.Log("ERROR: dialogueManager is null!");
            }
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerNearby = false;
            Debug.Log("Player left");
            dialogueManager.HidePressEPrompt();
        }
    }

    private void Update()
    {
        if (playerNearby && Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("F key pressed - starting dialogue");
            dialogueManager.StartDialogue();
        }
    }
}