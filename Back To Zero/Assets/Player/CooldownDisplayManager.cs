using UnityEngine;
using System.Collections.Generic;

public class CooldownDisplayManager : MonoBehaviour
{
    [SerializeField] private Transform cooldownShowcasePanel;
    [SerializeField] private GameObject cooldownHolderPrefab;
    
    private AbilitySetter abilitySetter;
    private Dictionary<AbilityHolder, GameObject> displays = new Dictionary<AbilityHolder, GameObject>();
    
    void Start()
    {
        abilitySetter = FindFirstObjectByType<AbilitySetter>();
        if (cooldownShowcasePanel == null)
            cooldownShowcasePanel = GameObject.Find("CooldownShowcase")?.transform;
    }
    
    void Update()
    {
        if (abilitySetter == null || cooldownShowcasePanel == null || cooldownHolderPrefab == null) return;
        
        RefreshDisplays();
    }
    
    private void RefreshDisplays()
    {
        List<AbilityHolder> activeHolders = GetActiveHolders();
        
        foreach (var kvp in displays)
        {
            if (!activeHolders.Contains(kvp.Key) || kvp.Key?.ability == null)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
                displays.Remove(kvp.Key);
            }
        }
        
        foreach (var holder in activeHolders)
        {
            if (holder?.ability != null && !displays.ContainsKey(holder))
            {
                GameObject display = Instantiate(cooldownHolderPrefab, cooldownShowcasePanel);
                CooldownDisplayItem item = display.GetComponent<CooldownDisplayItem>();
                if (item == null) item = display.AddComponent<CooldownDisplayItem>();
                item.Initialize(holder);
                displays[holder] = display;
            }
        }
    }
    
    private List<AbilityHolder> GetActiveHolders()
    {
        List<AbilityHolder> holders = new List<AbilityHolder>();
        if (abilitySetter == null) return holders;
        
        for (int i = 0; i < 4; i++)
        {
            AbilityHolder h = abilitySetter.GetAbilityHolder(i);
            if (h?.ability != null) holders.Add(h);
        }
        
        AbilityHolder dash = abilitySetter.GetDashHolder();
        if (dash?.ability != null && !holders.Contains(dash))
            holders.Add(dash);
        
        return holders;
    }
}

