using Abilities;
using Entities;
using TMPro;
using UnityEngine;

namespace UI
{
    public class AbilityInfoPanel : MonoBehaviour
    {
        [Header("Texts")] 
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private TextMeshProUGUI radiusText;
        [SerializeField] private TextMeshProUGUI stepsText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Colors")] 
        [SerializeField] private string energyColorHex = "765A43";
        [SerializeField] private string damageColorHex = "FF7A45";
        [SerializeField] private string radiusColorHex = "D4A373";
        
        [Header("Dynamic Steps Colors")]
        [SerializeField] private string stepsFullColorHex = "32CD32";    
        [SerializeField] private string stepsDamagedColorHex = "FF7A45"; 
        [SerializeField] private string stepsEmptyColorHex = "FF4444";   

        [Header("Root")] 
        [SerializeField] private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Show(AbilityData ability, BaseEntity actor)
        {
            // Debug.Log($"<color=lime>[AbilityInfoPanel.Show]</color> ability={ability?.abilityName}, actor={actor?.name}");

            if (ability == null) { Hide(); return; }
            if (actor == null)
            {
                Debug.LogWarning("[AbilityInfoPanel]");
                return;
            }

            headerText.text = "Выбрана способность:";
            nameText.text = $"[{ability.abilityName.ToUpper()}]";

            if (descriptionText != null)
            {
                descriptionText.text = ability.description;
                descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(ability.description));
            }

            var energy = ability.energyCost;
            if (energy > 0)
            {
                energyText.gameObject.SetActive(true);
                energyText.text = $"расход: <color=#{energyColorHex}>{energy}</color>";
            }
            else
            {
                energyText.gameObject.SetActive(false);
            }

            var stats = actor.Stats;
            var isMovementAbility = ability is MoveAbilityData;

            
            if (isMovementAbility && stats != null)
            {
                stepsText.gameObject.SetActive(true);
                
                var maxSteps = stats.MaxStepsPerRound;
                
                
                
                var currentSteps = Mathf.Min(stats.RemainingSteps, stats.Energy);

                
                string currentStepsColor;
                if (currentSteps <= 0)
                {
                    currentStepsColor = stepsEmptyColorHex;
                }
                else if (currentSteps < maxSteps)
                {
                    currentStepsColor = stepsDamagedColorHex;
                }
                else
                {
                    currentStepsColor = stepsFullColorHex;
                }

                stepsText.text = $"оставшиеся шаги: <color=#{currentStepsColor}>{currentSteps}</color>/{maxSteps}";
            }
            else
            {
                stepsText.gameObject.SetActive(false);
            }

            var dmg = ability.GetDisplayDamage(actor);
            if (dmg > 0 && !isMovementAbility)
            {
                damageText.gameObject.SetActive(true);
                damageText.text = $"урон: <color=#{damageColorHex}>{dmg}</color>";
            }
            else
            {
                damageText.gameObject.SetActive(false);
            }

            var minRadius = ability.displayMinRange;
            var maxRadius = ability.GetDisplayRange(actor);

            if (maxRadius > 0)
            {
                radiusText.gameObject.SetActive(true);

                if (minRadius > 0 && minRadius < maxRadius)
                {
                    radiusText.text = $"радиус: <color=#{radiusColorHex}>{minRadius}-{maxRadius}</color> кл.";
                }
                else
                {
                    radiusText.text = $"радиус: <color=#{radiusColorHex}>{maxRadius}</color> кл.";
                }
            }
            else
            {
                radiusText.gameObject.SetActive(false);
            }

            SetVisible(true);
        }

        public void Hide() => SetVisible(false);

        private void SetVisible(bool v)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = v ? 1 : 0;
                canvasGroup.blocksRaycasts = v;
            }
            else gameObject.SetActive(v);
        }
    }
}