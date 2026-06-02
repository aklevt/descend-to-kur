using System.Collections;
using UI.Dialogue;
using UnityEngine;

namespace Core.Room
{
    public class RoomDialogueTrigger : MonoBehaviour
    {
        [Header("Dialogue Settings")]
        [SerializeField] private DialogueData dialogueData;
        
        [SerializeField] private DialogueTriggerType triggerType = DialogueTriggerType.OnRoomEnter;
        
        [Tooltip("Задержка перед запуском диалога (секунды)")]
        [SerializeField] private float delayBefore = 0f;
        
        [Tooltip("Запускать диалог только один раз за комнату")]
        [SerializeField] private bool playOnce = true;

        private bool hasPlayed = false;
        private RoomController parentRoom;

        public DialogueData DialogueData => dialogueData;
        public DialogueTriggerType TriggerType => triggerType;

        private void Awake()
        {
            parentRoom = GetComponentInParent<RoomController>();
        }

        public void Initialize()
        {
            hasPlayed = false;

            // if (triggerType == DialogueTriggerType.OnRoomCleared && parentRoom != null)
            // {
            //     parentRoom.OnRoomCleared += TriggerDialogue;
            // }
        }

        public void Cleanup()
        {
            // if (parentRoom != null)
            // {
            //     parentRoom.OnRoomCleared -= TriggerDialogue;
            // }
        }

        public IEnumerator TriggerDialogueRoutine()
        {
            if (playOnce && hasPlayed) yield break;
            if (dialogueData == null) yield break;
            
            if (dialogueData.showOnlyOnce && DialogueProgress.IsCompleted(dialogueData.DialogueID))
            {
                yield break;
            }

            hasPlayed = true;
            
            if (delayBefore > 0f)
            {
                yield return new WaitForSeconds(delayBefore);
            }

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(dialogueData);
                
                yield return new WaitUntil(() => DialogueManager.Instance.IsDialogueActive);
            }
        }

        private System.Collections.IEnumerator DelayedStart()
        {
            yield return new WaitForSeconds(delayBefore);
            DialogueManager.Instance?.StartDialogue(dialogueData);
        }
    }

    public enum DialogueTriggerType
    {
        OnRoomEnter,    // При входе в комнату
        OnRoomCleared,  // После победы
        Manual          // Вручную
    }
}