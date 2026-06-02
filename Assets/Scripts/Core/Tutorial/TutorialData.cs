using System.Collections.Generic;
using UnityEngine;

namespace Core.Tutorial
{
    [CreateAssetMenu(fileName = "NewTutorialData", menuName = "Tutorial/Tutorial Data")]
    public class TutorialData : ScriptableObject
    {
        public List<TutorialStep> steps;
    }
}