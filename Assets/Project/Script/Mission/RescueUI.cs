using UnityEngine;
using UnityEngine.UI;

public class RescueUI : MonoBehaviour
{
    public static RescueUI Instance;

    public Slider progressBar;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void UpdateProgress(float value)
    {
        progressBar.value = Mathf.Clamp01(value);
    }
}
