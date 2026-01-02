using TMPro;
using UnityEngine;

public class ClassDescriptionPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text classTitle;
    [SerializeField] private TMP_Text classTagline;
    [SerializeField] private TMP_Text taleText;

    public void SetKnight()
    {
        classTitle.text = "Knight";
        classTagline.text = "Steel-bound defender of the realm";
        taleText.text =
            "Raised in the old kingdoms, the Knight stands where others fall. " +
            "Trained to hold the line against monsters and men, they protect allies " +
            "through discipline, armor, and unbreakable will. " +
            "A Knight doesn't chase glory - they endure until victory.";
    }

    public void SetElf()
    {
        classTitle.text = "Elf";
        classTagline.text = "Swift guardian of the ancient wilds";
        taleText.text =
            "Born beneath towering canopies and moonlit ruins, the Elf moves with calm precision. " +
            "They listen to the forest, strike from afar, and vanish before danger closes in. " +
            "An Elf fights with patience - every arrow is a promise, and every step leaves no trace.";
    }

    public void SetMage()
    {
        classTitle.text = "Mage";
        classTagline.text = "Scholar of the arcane and master of elements";
        taleText.text =
            "The Mage bends reality through knowledge carved into ritual and discipline. " +
            "Where steel fails, magic reshapes the battlefield - freezing, burning, or breaking the will of foes. " +
            "A Mage wins by foresight: controlling the fight before it even begins.";
    }
}