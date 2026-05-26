using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core;

namespace UI.Dialogue
{
    /// <summary>
    /// Управляет диалоговой системой
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("UI Views")]
        [SerializeField] private SubtitleDialogueUI subtitleUI;
        [SerializeField] private InGameDialogueUI inGameUI;
        [SerializeField] private FullscreenDialogueUI fullscreenUI;
        
        [Header("Settings")]
        [SerializeField] private float inputDebounceDelay = 0.5f;

        private Dictionary<DialogueStyle, IDialogueView> dialogueViews;
        
        private DialogueState dialogueState = new();
        
        public bool IsDialogueActive => dialogueState.IsActive;

        private void Awake()
        {
            if (Instance == null) 
            {
                Instance = this;
            }
            else 
            {
                Destroy(gameObject);
                return;
            }
            
            dialogueViews = new Dictionary<DialogueStyle, IDialogueView>();
            InitializeViews();
        }

        // private void Update()
        // {
        //     if (!dialogueState.IsActive) return;
        //     
        //     HandleDialogueInput();
        // }

        private void InitializeViews()
        {
            if (subtitleUI != null) dialogueViews.Add(DialogueStyle.Subtitle, subtitleUI);
            if (inGameUI != null) dialogueViews.Add(DialogueStyle.InGame, inGameUI);
            if (fullscreenUI != null) dialogueViews.Add(DialogueStyle.Fullscreen, fullscreenUI);
        }

        private void HandleDialogueInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SkipDialogue();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                AdvanceDialogue();
            }
        }

        public void StartDialogue(DialogueData data)
        {
            if (!CanStartDialogue(data)) return;

            if (!dialogueViews.TryGetValue(data.style, out var view)) return;
            
            if (view is MonoBehaviour viewMB)
            {
                viewMB.gameObject.SetActive(true);
            }

            dialogueState.Initialize(data, view, inputDebounceDelay);
            GameStateManager.Instance?.SetState(GameState.Dialog);
            StartCoroutine(DialogueSequence());
        }

        public void AdvanceDialogue()
        {
            if (!dialogueState.CanAdvance()) return;

            if (dialogueState.CurrentView.IsTyping)
            {
                dialogueState.CurrentView.CompleteCurrentLine();
            }
            else if (dialogueState.IsWaitingForInput)
            {
                dialogueState.ContinueToNext();
            }
        }

        public void SkipDialogue()
        {
            if (!dialogueState.CanSkip()) return;

            dialogueState.RequestSkip();
            
            if (dialogueState.DialogueCoroutine != null)
            {
                StopCoroutine(dialogueState.DialogueCoroutine);
            }

            StartCoroutine(SkipRoutine());
        }

        private IEnumerator DialogueSequence()
        {
            var coroutine = StartCoroutine(RunDialogue());
            dialogueState.SetDialogueCoroutine(coroutine);
            yield return coroutine;
        }

        private IEnumerator RunDialogue()
        {
            yield return dialogueState.CurrentView.Show();

            while (dialogueState.HasMoreLines())
            {
                var line = dialogueState.GetCurrentLine();
                
                dialogueState.StartWaitingForInput();
                yield return dialogueState.CurrentView.DisplayLine(line, dialogueState.Data.typewriterSpeed);
                yield return new WaitUntil(() => !dialogueState.IsWaitingForInput);
                
                dialogueState.MoveToNextLine();
            }

            yield return dialogueState.CurrentView.Hide();
            EndDialogue();
        }

        private IEnumerator SkipRoutine()
        {
            if (dialogueState.CurrentView != null)
            {
                yield return dialogueState.CurrentView.Hide();
            }
            
            EndDialogue();
        }

        private void EndDialogue()
        {
            if (dialogueState.Data != null && dialogueState.Data.showOnlyOnce)
            {
                DialogueProgress.MarkCompleted(dialogueState.Data.DialogueID);
            }
            
            dialogueState.Reset();
            GameStateManager.Instance?.ReturnToGameplay();
        }

        private bool CanStartDialogue(DialogueData data)
        {
            if (data?.lines == null || data.lines.Count == 0)
            {
                Debug.LogWarning("[DialogueManager] Попытка запустить пустой диалог");
                return false;
            }
            
            if (dialogueState.IsActive)
            {
                Debug.LogWarning("[DialogueManager] Диалог уже активен");
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Состояние диалоговой системы
    /// </summary>
    internal class DialogueState
    {
        public DialogueData Data { get; private set; }
        public IDialogueView CurrentView { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsWaitingForInput { get; private set; }
        public bool IsClosing { get; private set; }
        public Coroutine DialogueCoroutine { get; private set; }
        
        private int currentLineIndex;
        private float inputBlockedUntil;

        public void Initialize(DialogueData data, IDialogueView view, float inputDebounce)
        {
            Data = data;
            CurrentView = view;
            IsActive = true;
            IsWaitingForInput = false;
            IsClosing = false;
            currentLineIndex = 0;
            inputBlockedUntil = Time.time + inputDebounce;
            DialogueCoroutine = null;
        }

        public void Reset()
        {
            Data = null;
            CurrentView = null;
            IsActive = false;
            IsWaitingForInput = false;
            IsClosing = false;
            currentLineIndex = 0;
            inputBlockedUntil = 0f;
            DialogueCoroutine = null;
        }

        public bool CanAdvance()
        {
            return IsActive && !IsClosing && Time.time >= inputBlockedUntil;
        }

        public bool CanSkip()
        {
            return IsActive && !IsClosing && Time.time >= inputBlockedUntil && 
                   (Data?.canSkip == true);
        }

        public void RequestSkip()
        {
            IsClosing = true;
        }

        public bool HasMoreLines()
        {
            return Data != null && currentLineIndex < Data.lines.Count;
        }

        public DialogueLine GetCurrentLine()
        {
            return Data?.lines[currentLineIndex];
        }

        public void MoveToNextLine()
        {
            currentLineIndex++;
        }

        public void StartWaitingForInput()
        {
            IsWaitingForInput = true;
        }

        public void ContinueToNext()
        {
            IsWaitingForInput = false;
        }

        public void SetDialogueCoroutine(Coroutine coroutine)
        {
            DialogueCoroutine = coroutine;
        }
    }
}