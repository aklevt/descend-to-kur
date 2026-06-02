using UnityEngine;

namespace UI.Menu
{
    [CreateAssetMenu(fileName = "IntroData", menuName = "Menu/Intro Data")]
    public class IntroData : ScriptableObject
    {
        [Header("Intro Content")]
        [TextArea(2, 4)]
        public string[] introTexts = new string[]
        {
            
        };
        
        [Header("Settings")]
        [Range(10f, 100f)]
        public float typeSpeed = 50f;
        
        [Tooltip("Название сцены геймплея")]
        public string gameplaySceneName = "SampleScene";
    }
}