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
    { 
      // Trigger fade animation
      fadeAnimator.SetTrigger("isFadeOut"); 
      // Wait for fade duration (match your animation length)
      yield return new WaitForSeconds(3f); 
      // Show loading panel
      panelMainMenu.SetActive(false); 
      panelLoading.SetActive(true); 
      
      // Reset progress bar
      if (progressBar != null) progressBar.value = 0f;
      
      // Start async scene load
      AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("IntroScene"); 
      asyncLoad.allowSceneActivation = false;
      
      float minimumLoadTime = 1.5f; // Minimum seconds to show loading
      float elapsedTime = 0f;
      
      while (!asyncLoad.isDone) 
      { 
          elapsedTime += Time.deltaTime;
          
          // Update progress bar (smooth fill based on time)
          float progress = Mathf.Min(elapsedTime / minimumLoadTime, asyncLoad.progress / 0.9f);
          if (progressBar != null) progressBar.value = progress;
          
          // When ready AND minimum time passed, activate scene
          if (asyncLoad.progress >= 0.9f && elapsedTime >= minimumLoadTime) 
          {
              if (progressBar != null) progressBar.value = 1f;
              yield return new WaitForSeconds(0.5f); // Brief pause at 100%
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