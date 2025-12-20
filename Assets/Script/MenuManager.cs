using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public GameObject panelMainMenu;
    public GameObject panelSettings;
    public GameObject panelCredits;
    public GameObject panelInstructions;

    public void StartGame()
    {
        SceneManager.LoadScene("mainScene");
    }

    public void OpenSettings()
    {
        panelMainMenu.SetActive(false);
        panelSettings.SetActive(true);
    }

    public void OpenCredits()
    {
        panelMainMenu.SetActive(false);
        panelCredits.SetActive(true);
    }

    public void OpenInstructions()
    {
        panelMainMenu.SetActive(false);
        panelInstructions.SetActive(true);
    }

    public void BackToMenu()
    {
        panelSettings.SetActive(false);
        panelCredits.SetActive(false);
        panelInstructions.SetActive(false);
        panelMainMenu.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Game quit");
        UnityEditor.EditorApplication.isPlaying = false;
        Application.Quit();
    }
}
