using UnityEngine;
using UI.Dialogue;

namespace Intro
{
    [System.Serializable]
    public class IntroStep
    {
        public StepType type;


        [TextArea(3, 10)] public string narrativeText;


        public DialogueData dialogue;


        public Sprite image;
        [TextArea(1, 3)] public string imageCaption;


        public float delayAfter = 0.5f;
    }

    public enum StepType
    {
        Narrative,
        Dialogue,
        Image
    }
}