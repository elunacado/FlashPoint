using UnityEngine;
using TMPro;

public class UIController : MonoBehaviour
{
    public TextMeshProUGUI savedVictimsText;

    public void UpdateSavedVictims(int savedVictims)
    {
        savedVictimsText.text = $"Saved Victims: {savedVictims}";
    }
}
