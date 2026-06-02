using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class StatBadge : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI valueText;

        public void Setup(Sprite icon, string text, Color textColor)
        {
            iconImage.sprite = icon;
            valueText.text = text;
            valueText.color = textColor;
        }
    }
}