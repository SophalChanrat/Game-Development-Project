using UnityEngine;
using UnityEngine.UI;

public class RescueInteractable : MonoBehaviour
{
    [Header("Rescue Settings")]
    public float rescueDuration = 5f;

    [Header("References")]
    public AnimalMovement trappedAnimal;
    public ParticleSystem cageParticles;
    public Slider worldSlider;   // Slider above the animal

    [HideInInspector] public bool playerInRange = false;

    private float progress;
    private bool isRescuing;
    private bool rescued;

    private void Start()
    {
        if (worldSlider != null)
            worldSlider.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isRescuing || rescued)
            return;

        progress += Time.deltaTime;

        if (worldSlider != null)
            worldSlider.value = progress / rescueDuration;

        if (progress >= rescueDuration)
            CompleteRescue();
    }

    public void TryRescue()
    {
        if (playerInRange)
            StartRescue();
    }

    public void StartRescue()
    {
        if (rescued || isRescuing)
            return;

        Debug.Log("Rescue started.");
        isRescuing = true;
        progress = 0f;

        if (worldSlider != null)
        {
            worldSlider.value = 0f;
            worldSlider.gameObject.SetActive(true);
        }
    }

    public void CancelRescue()
    {
        if (!isRescuing)
            return;

        Debug.Log("Rescue canceled.");
        isRescuing = false;
        progress = 0f;

        if (worldSlider != null)
        {
            worldSlider.value = 0f;
            worldSlider.gameObject.SetActive(false);
        }
    }

    private void CompleteRescue()
    {
        Debug.Log("Rescue completed!");
        rescued = true;
        isRescuing = false;

        if (worldSlider != null)
            worldSlider.gameObject.SetActive(false);

        if (trappedAnimal != null)
            trappedAnimal.Release();

        if (cageParticles != null)
            cageParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            other.GetComponent<PlayerMovement3D>().currentRescueTarget = this;
            Debug.Log("Player entered rescue zone.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            other.GetComponent<PlayerMovement3D>().currentRescueTarget = null;
            CancelRescue();
            Debug.Log("Player left rescue zone.");
        }
    }
}
