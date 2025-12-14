using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages a single skill UI element with icon, cooldown overlay, and text
/// </summary>
public class SkillUI : MonoBehaviour
{
    [Header("UI References")]
    public Image skillIcon;              // The skill icon image
    public Image cooldownOverlay;        // Dark overlay when on cooldown
    public TextMeshProUGUI cooldownText; // Text showing remaining cooldown
    public TextMeshProUGUI hotkeyText;   // Text showing hotkey (Q, E, R)
    
    [Header("Visual Settings")]
    public Color readyColor = Color.white;
    public Color cooldownColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public bool showCooldownText = true;
    public bool showHotkey = true;
    
    private bool isOnCooldown = false;
    
    void Start()
    {
        // Initialize UI state
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = 0f;
        }
        
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Updates the skill UI based on cooldown state
    /// </summary>
    /// <param name="isReady">Is the skill ready to use?</param>
    /// <param name="cooldownRemaining">Remaining cooldown time</param>
    /// <param name="cooldownPercentage">Cooldown percentage (0-1)</param>
    public void UpdateSkillUI(bool isReady, float cooldownRemaining, float cooldownPercentage)
    {
        isOnCooldown = !isReady;
        
        // Update icon color
        if (skillIcon != null)
        {
            skillIcon.color = isReady ? readyColor : cooldownColor;
        }
        
        // Update cooldown overlay (radial fill)
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = isReady ? 0f : (1f - cooldownPercentage);
        }
        
        // Update cooldown text
        if (cooldownText != null && showCooldownText)
        {
            if (isReady)
            {
                cooldownText.gameObject.SetActive(false);
            }
            else
            {
                cooldownText.gameObject.SetActive(true);
                cooldownText.text = cooldownRemaining.ToString("F1") + "s";
            }
        }
    }
    
    /// <summary>
    /// Sets the skill icon sprite
    /// </summary>
    public void SetSkillIcon(Sprite icon)
    {
        if (skillIcon != null && icon != null)
        {
            skillIcon.sprite = icon;
        }
    }
    
    /// <summary>
    /// Sets the hotkey text (Q, E, R, etc.)
    /// </summary>
    public void SetHotkeyText(string hotkey)
    {
        if (hotkeyText != null && showHotkey)
        {
            hotkeyText.text = hotkey;
            hotkeyText.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Pulses the icon when skill is cast (optional visual feedback)
    /// </summary>
    public void PlayCastAnimation()
    {
        // You can add animation here (scale pulse, color flash, etc.)
        if (skillIcon != null)
        {
            StartCoroutine(PulseIcon());
        }
    }
    
    private System.Collections.IEnumerator PulseIcon()
    {
        Vector3 originalScale = skillIcon.transform.localScale;
        Vector3 targetScale = originalScale * 1.2f;
        
        float duration = 0.1f;
        float elapsed = 0f;
        
        // Scale up
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            skillIcon.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }
        
        elapsed = 0f;
        
        // Scale down
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            skillIcon.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
        
        skillIcon.transform.localScale = originalScale;
    }
}
