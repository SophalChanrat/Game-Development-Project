using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerSkillUI : MonoBehaviour
{
    [Header("References")]
    public PlayerSkillSystem playerSkillSystem;
    
    [Header("Skill 1 UI")]
    public Image skill1Icon;
    public Image skill1Cooldown;
    public TextMeshProUGUI skill1CooldownText;
    public TextMeshProUGUI skill1Hotkey;
    
    [Header("Skill 2 UI")]
    public Image skill2Icon;
    public Image skill2Cooldown;
    public TextMeshProUGUI skill2CooldownText;
    public TextMeshProUGUI skill2Hotkey;
    
    [Header("Skill 3 UI")]
    public Image skill3Icon;
    public Image skill3Cooldown;
    public TextMeshProUGUI skill3CooldownText;
    public TextMeshProUGUI skill3Hotkey;
    
    void Start()
    {
        if (playerSkillSystem == null)
        {
            playerSkillSystem = FindObjectOfType<PlayerSkillSystem>();
        }
        
        // Set hotkey texts (now using Input System bindings)
        if (skill1Hotkey != null) skill1Hotkey.text = "Q";
        if (skill2Hotkey != null) skill2Hotkey.text = "E";
        if (skill3Hotkey != null) skill3Hotkey.text = "R";
    }
    
    void Update()
    {
        if (playerSkillSystem == null) return;
        
        UpdateSkillUI(playerSkillSystem.skill1, skill1Cooldown, skill1CooldownText);
        UpdateSkillUI(playerSkillSystem.skill2, skill2Cooldown, skill2CooldownText);
        UpdateSkillUI(playerSkillSystem.skill3, skill3Cooldown, skill3CooldownText);
    }
    
    void UpdateSkillUI(Skill skill, Image cooldownOverlay, TextMeshProUGUI cooldownText)
    {
        if (skill.IsOnCooldown())
        {
            // Show cooldown overlay
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = 1f - skill.GetCooldownProgress();
            }
            
            // Show cooldown text
            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(true);
                cooldownText.text = skill.GetCooldownRemaining().ToString("F1");
            }
        }
        else
        {
            // Hide cooldown overlay
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = 0f;
            }
            
            // Hide cooldown text
            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(false);
            }
        }
    }
    
    // Call this method to set skill icons
    public void SetSkillIcon(int skillNumber, Sprite icon)
    {
        switch (skillNumber)
        {
            case 1:
                if (skill1Icon != null) skill1Icon.sprite = icon;
                break;
            case 2:
                if (skill2Icon != null) skill2Icon.sprite = icon;
                break;
            case 3:
                if (skill3Icon != null) skill3Icon.sprite = icon;
                break;
        }
    }
}
