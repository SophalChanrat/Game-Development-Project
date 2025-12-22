using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public class MenuManager : MonoBehaviour
{
    public GameObject panelMainMenu;
    public GameObject panelSettings;
    public GameObject panelCredits;
    public GameObject panelInstructions;
    public GameObject panelLoading;
    public Animator fadeAnimator; // assign Image_Fade Animator
    public UnityEngine.UI.Slider progressBar; 

    public void StartGame()
    {
        StartCoroutine(LoadGameScene());
    }

    public void OpenSettings()
    {
        panelMainMenu.SetActive(false);
        panelSettings.SetActive(true);
    }
    private IEnumerator LoadGameScene()
    { // Trigger fade animation
      fadeAnimator.SetTrigger("isFadeOut"); 
      // Wait for fade duration (match your animation length)
      yield return new WaitForSeconds(1f); 
      // Show loading panel
      panelMainMenu.SetActive(false); 
      panelLoading.SetActive(true); 
      // Start async scene load
      AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("mainScene"); 
      asyncLoad.allowSceneActivation = false; 
      while (!asyncLoad.isDone) { 
      // Update progress bar if present
      if (progressBar != null) progressBar.value = Mathf.Clamp01(asyncLoad.progress / 0.9f);
      // When ready, activate scene
      if (asyncLoad.progress >= 0.9f) {
         asyncLoad.allowSceneActivation = true; 
         } 
       yield return null; 
      } 
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
        fadeAnimator.SetTrigger("isFadeIn");
    }

    public void QuitGame()
    {
        Debug.Log("Game quit");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
