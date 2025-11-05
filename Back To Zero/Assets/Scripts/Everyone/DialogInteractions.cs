using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[System.Serializable]
public class DialogLine
{
    public enum SpeakerType
    {
        NPC,
        Player,
        Custom
    }
    
    [Header("Speaker Selection")]
    public SpeakerType speakerType = SpeakerType.NPC;
    public string customSpeakerName = "";
    
    [Header("Speaker Information")]
    public Color speakerColor = Color.white;
    
    [Header("Dialog Content")]
    [TextArea(3, 10)]
    public string dialogText = "";
    
    [Header("Optional Settings")]
    public float customTextSpeed = -1f;
    public AudioClip voiceClip;
    
    public string GetSpeakerName()
    {
        switch (speakerType)
        {
            case SpeakerType.NPC:
                return "NPC";
            case SpeakerType.Player:
                return "Player";
            case SpeakerType.Custom:
                return string.IsNullOrEmpty(customSpeakerName) ? "Unknown" : customSpeakerName;
            default:
                return "Unknown";
        }
    }
    
    public Color GetSpeakerColor(DialogInteractions dialogInteractions = null)
    {
        Color resultColor;
        
        if (dialogInteractions != null)
        {
            switch (speakerType)
            {
                case SpeakerType.NPC:
                    resultColor = dialogInteractions.npcDefaultColor;
                    break;
                case SpeakerType.Player:
                    resultColor = dialogInteractions.playerDefaultColor;
                    break;
                case SpeakerType.Custom:
                    resultColor = speakerColor;
                    break;
                default:
                    resultColor = new Color(0f, 0f, 0f, 1f);
                    break;
            }
        }
        else
        {
            switch (speakerType)
            {
                case SpeakerType.NPC:
                    resultColor = new Color(0f, 0f, 0f, 1f);
                    break;
                case SpeakerType.Player:
                    resultColor = new Color(0f, 0f, 0f, 1f);
                    break;
                case SpeakerType.Custom:
                    resultColor = speakerColor;
                    break;
                default:
                    resultColor = new Color(0f, 0f, 0f, 1f);
                    break;
            }
        }
        
        resultColor.a = 1f;
        return resultColor;
    }
    
    public DialogLine()
    {
        speakerType = SpeakerType.NPC;
        customSpeakerName = "";
        dialogText = "";
        speakerColor = new Color(0f, 0f, 0f, 1f);
        customTextSpeed = -1f;
    }
    
    public DialogLine(SpeakerType speaker, string text)
    {
        speakerType = speaker;
        dialogText = text;
        speakerColor = new Color(0f, 0f, 0f, 1f);
        customTextSpeed = -1f;
    }
    
    public DialogLine(string customSpeaker, string text, Color color)
    {
        speakerType = SpeakerType.Custom;
        customSpeakerName = customSpeaker;
        dialogText = text;
        speakerColor = new Color(color.r, color.g, color.b, 1f);
        customTextSpeed = -1f;
    }
}

public class DialogInteractions : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 3f;
    public float cancelRange = 5f;
    public KeyCode interactionKey = KeyCode.E;
    public LayerMask playerLayer = 1;
    
    [Header("UI References")]
    public GameObject interactionIndicator;
    public GameObject dialogBox;
    public TextMeshProUGUI dialogTextMesh;
    public TextMeshProUGUI titleTextMesh;
    
    [Header("Dialog Settings")]
    public DialogLine[] npcDialogLines;
    public float textSpeed = 0.05f;
    
    [Header("Color Settings")]
    public Color npcDefaultColor = new Color(0f, 0f, 0f, 1f);
    public Color playerDefaultColor = new Color(0f, 0f, 0f, 1f);
    public Color customDefaultColor = new Color(0f, 0f, 0f, 1f);
    public Color dialogTextColor = new Color(0f, 0f, 0f, 1f);
    
    private bool playerInRange = false;
    private bool dialogActive = false;
    private Transform playerTransform;
    private int currentDialogIndex = 0;
    private Coroutine typingCoroutine;
    
    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        if (interactionIndicator != null)
        {
            interactionIndicator.SetActive(false);
        }
        
        if (cancelRange <= interactionRange)
        {
            cancelRange = interactionRange + 2f;
        }
        
        if (dialogBox != null)
        {
            dialogBox.SetActive(false);
        }
        else
        {
            if (dialogTextMesh != null)
            {
                dialogTextMesh.gameObject.SetActive(false);
            }
            if (titleTextMesh != null)
            {
                titleTextMesh.gameObject.SetActive(false);
            }
        }
    }
    
    void Update()
    {
        if (playerTransform == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        bool wasInRange = playerInRange;
        playerInRange = distanceToPlayer <= interactionRange;
        
        if (dialogActive && distanceToPlayer > cancelRange)
        {
            CancelDialog();
        }
        
        if (interactionIndicator != null)
        {
            interactionIndicator.SetActive(playerInRange && !dialogActive);
        }
        
        if (playerInRange && !dialogActive && Input.GetKeyDown(interactionKey))
        {
            StartInteraction();
        }
        
        if (dialogActive)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                if (typingCoroutine != null)
                {
                    StopCoroutine(typingCoroutine);
                    typingCoroutine = null;
                    ShowFullCurrentDialog();
                }
                else
                {
                    NextDialogLine();
                }
            }
        }
    }
    
    void StartInteraction()
    {
        if (!dialogActive && npcDialogLines != null && npcDialogLines.Length > 0)
        {
            dialogActive = true;
            currentDialogIndex = 0;
            
            if (dialogBox != null)
            {
                dialogBox.SetActive(true);
                
                if (titleTextMesh != null && !titleTextMesh.gameObject.activeInHierarchy)
                {
                    titleTextMesh.gameObject.SetActive(true);
                }
                if (dialogTextMesh != null && !dialogTextMesh.gameObject.activeInHierarchy)
                {
                    dialogTextMesh.gameObject.SetActive(true);
                }
            }
            else
            {
                if (dialogTextMesh != null)
                {
                    dialogTextMesh.gameObject.SetActive(true);
                }
                if (titleTextMesh != null)
                {
                    titleTextMesh.gameObject.SetActive(true);
                }
            }
            
            ShowCurrentDialogLine();
        }
    }
    
    void ShowCurrentDialogLine()
    {
        if (npcDialogLines != null && currentDialogIndex < npcDialogLines.Length)
        {
            DialogLine currentLine = npcDialogLines[currentDialogIndex];
            
            if (titleTextMesh != null)
            {
                string speakerName = currentLine.GetSpeakerName();
                titleTextMesh.text = speakerName;
                titleTextMesh.color = currentLine.GetSpeakerColor(this);
            }
            
            if (dialogTextMesh != null)
            {
                dialogTextMesh.text = "";
                dialogTextMesh.color = dialogTextColor;
                float speed = currentLine.customTextSpeed > 0 ? currentLine.customTextSpeed : textSpeed;
                typingCoroutine = StartCoroutine(TypeText(currentLine.dialogText, speed));
            }
        }
    }
    
    void ShowFullCurrentDialog()
    {
        if (npcDialogLines != null && currentDialogIndex < npcDialogLines.Length)
        {
            if (dialogTextMesh != null)
            {
                dialogTextMesh.text = npcDialogLines[currentDialogIndex].dialogText;
            }
        }
    }
    
    void NextDialogLine()
    {
        if (npcDialogLines != null && currentDialogIndex < npcDialogLines.Length - 1)
        {
            currentDialogIndex++;
            ShowCurrentDialogLine();
        }
        else
        {
            EndDialog();
        }
    }
    
    IEnumerator TypeText(string text, float speed)
    {
        if (dialogTextMesh == null) yield break;
        
        dialogTextMesh.text = "";
        foreach (char c in text.ToCharArray())
        {
            dialogTextMesh.text += c;
            yield return new WaitForSeconds(speed);
        }
        typingCoroutine = null;
    }
    
    void EndDialog()
    {
        dialogActive = false;
        currentDialogIndex = 0;
        
        if (dialogBox != null)
        {
            dialogBox.SetActive(false);
        }
        else
        {
            if (dialogTextMesh != null)
            {
                dialogTextMesh.gameObject.SetActive(false);
            }
            if (titleTextMesh != null)
            {
                titleTextMesh.gameObject.SetActive(false);
            }
        }
        
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
    }
    
    void CancelDialog()
    {
        EndDialog();
    }
    
    public void SetNPCDialogLines(DialogLine[] newDialogLines)
    {
        npcDialogLines = newDialogLines;
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, cancelRange);
    }
}
