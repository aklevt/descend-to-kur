using System;
using UnityEngine;

namespace UI.Dialogue
{
    public enum DialogueTextAlignment
    {
        Top,
        Center
    }

    [Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string text;
        
        [Tooltip("Имя говорящего")]
        public string speakerName;
        
        [Tooltip("Портрет говорящего или картинка")]
        public Sprite speakerPortrait;
        
        [Tooltip("Показывать картинку в RightLayout независимо от наличия текста")]
        public bool showInlineImage;

        [Tooltip("Выравнивание текста (только для текста автора в RightLayout)")]
        public DialogueTextAlignment textAlignment = DialogueTextAlignment.Top;
        
        [Tooltip("Задержка после показа реплики (секунды)")]
        public float delayAfter = 0.5f;
    }
}