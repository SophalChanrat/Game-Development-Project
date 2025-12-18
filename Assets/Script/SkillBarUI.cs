using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the entire skill bar UI at the bottom center of the screen
/// Connects to the SkillSystem and updates the UI accordingly
/// </summary>
public class SkillBarUI : MonoBehaviour
{
    [Header("Skill System Reference")]
    public SkillSystem skillSystem;
    
    [Header("Skill UI References")]
    public SkillUI skill1UI;
    public SkillUI skill2UI;
    public SkillUI skill3UI;
    
    [Header("Skill Icons (Optional)")]
    public Sprite skill1Icon;
    public Sprite skill2Icon;
    public Sprite skill3Icon;
    
    [Header("Settings")]
    [Tooltip("If false, won't override hotkey text you set manually in the editor")]
    public bool overrideHotkeyText = false;
    
    void Start()
    {
        // Auto-find SkillSystem if not assigned
        if (skillSystem == null)
        {
            skillSystem = FindObjectOfType<SkillSystem>();
            if (skillSystem == null)
            {
                Debug.LogError("SkillBarUI: SkillSystem not found! Please assign it in the Inspector.");
                enabled = false;
                return;
            }
        }
        
        // Initialize skill icons (only if assigned)
        InitializeSkillIcons();
        
        // Only set hotkeys if override is enabled
        if (overrideHotkeyText)
        {
            InitializeHotkeyText();
        }
    }
    
    void Update()
    {
        if (skillSystem == null) return;
        
        // Update Skill 1 UI
        if (skill1UI != null)
        {
            skill1UI.UpdateSkillUI(
                skillSystem.IsSkill1Ready(),
                skillSystem.GetSkill1Cooldown(),
                skillSystem.GetSkill1CooldownPercentage()
            );
        }
        
        // Update Skill 2 UI
        if (skill2UI != null)
        {
            skill2UI.UpdateSkillUI(
                skillSystem.IsSkill2Ready(),
                skillSystem.GetSkill2Cooldown(),
                skillSystem.GetSkill2CooldownPercentage()
            );
        }
        
        // Update Skill 3 UI
        if (skill3UI != null)
        {
            skill3UI.UpdateSkillUI(
                skillSystem.IsSkill3Ready(),
                skillSystem.GetSkill3Cooldown(),
                skillSystem.GetSkill3CooldownPercentage()
            );
        }
    }
    
    void InitializeSkillIcons()
    {
        // Set skill icons (only if provided)
        if (skill1UI != null && skill1Icon != null)
        {
            skill1UI.SetSkillIcon(skill1Icon);
        }
        
        if (skill2UI != null && skill2Icon != null)
        {
            skill2UI.SetSkillIcon(skill2Icon);
        }
        
        if (skill3UI != null && skill3Icon != null)
        {
            skill3UI.SetSkillIcon(skill3Icon);
        }
    }
    
    void InitializeHotkeyText()
    {
        // Only call this if you want to override the text set in the editor
        if (skill1UI != null)
        {
            skill1UI.SetHotkeyText("Q");
        }
        
        if (skill2UI != null)
        {
            skill2UI.SetHotkeyText("E");
        }
        
        if (skill3UI != null)
        {
            skill3UI.SetHotkeyText("R");
        }
    }
}
