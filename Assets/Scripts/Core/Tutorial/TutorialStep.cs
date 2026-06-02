using System;
using UnityEngine;

namespace Core.Tutorial
{
    [Serializable]
    public struct TutorialStep
    {
        public string speakerName;
        [TextArea(3, 5)] public string text;
        [TextArea(1, 2)] public string objective;
        public Sprite speakerAvatar;

        [Header("Interactivity")]
        public TutorialActionType requiredAction;
        public int targetIndex;
        public Vector2Int targetCell;

        [Header("Auto Execution (On Step Start)")]
        public bool hasAutoTrigger;
        public string triggerName;
    }
}