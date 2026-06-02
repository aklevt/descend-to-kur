using Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD
{
    public class PlayerHUD : MonoBehaviour
    {
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Slider energySlider;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI energyText;
        
        [SerializeField] private TextMeshProUGUI stepsText;
        [SerializeField] private Slider stepsSlider;

        private int lastSteps = -1;
        private int lastEnergy;

        private void Update()
        {
            if (PlayerMovement.Instance == null) return;

            var stats = PlayerMovement.Instance.Stats;

            if (hpSlider != null)
            {
                hpSlider.maxValue = stats.MaxHealth;
                hpSlider.value = stats.Health;
            }

            if (energySlider != null)
            {
                energySlider.maxValue = stats.MaxEnergy;
                energySlider.value = stats.Energy;
            }

            if (hpText != null) 
                hpText.text = $"{stats.Health}/{stats.MaxHealth}";
            
            if (energyText != null) 
                energyText.text = $"{stats.Energy}/{stats.MaxEnergy}";
            
            var maxSteps = stats.MaxStepsPerRound;
            var actualSteps = Mathf.Min(stats.RemainingSteps, stats.Energy);
            
            if (stepsText != null) 
            {
                if (stats.RemainingSteps != lastSteps)
                {
                    Debug.Log($"<color=cyan>[PlayerHUD]</color> Шаги обновлены: {lastSteps} -> {stats.RemainingSteps}");
                    lastSteps = stats.RemainingSteps;
                }
                
                stepsText.text = $"{stats.RemainingSteps}";
                stepsText.color = stats.RemainingSteps > 0 ? Color.white : Color.gray;
            }
            
            if (stepsSlider != null)
            {
                stepsSlider.maxValue = maxSteps;
                stepsSlider.value = actualSteps;
            }
        }
    }
}