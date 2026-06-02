using System.Collections.Generic;
using UnityEngine;

namespace Intro
{
    [CreateAssetMenu(fileName = "IntroSequence", menuName = "Intro/Intro Sequence")]
    public class IntroSequence : ScriptableObject
    {
        [Header("Steps")]
        public List<IntroStep> steps = new List<IntroStep>();
        
        [Header("Settings")]
        [Range(20f, 100f)]
        public float narrativeTypeSpeed = 50f;
        
        public bool canSkip = true;
        
        [Header("After Intro")]
        public string sceneToLoad = "SampleScene";
    }
}