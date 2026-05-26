using System;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Управляет общим состоянием игры
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Gameplay;
        public event Action<GameState> OnStateChanged;

        public bool CanPlayerAct => CurrentState == GameState.Gameplay;
        public bool CanCameraMove => CurrentState is GameState.Gameplay or GameState.Dialog or GameState.Tutorial;
        public bool CanPause => CurrentState == GameState.Gameplay;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // DontDestroyOnLoad(gameObject);
            }
            else
                Destroy(gameObject);
        }

        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;

            var prev = CurrentState;
            CurrentState = newState;

            Debug.Log($"<color=cyan>[GameState]</color> {prev} -> {newState}");
            
            ApplyStateEffects(newState);
            OnStateChanged?.Invoke(newState);
        }

        private void ApplyStateEffects(GameState state)
        {
            switch (state)
            {
                case GameState.Paused:
                case GameState.Tutorial:
                    Time.timeScale = 0f;
                    break;
                    
                case GameState.Gameplay:
                case GameState.Dialog:
                case GameState.Transition:
                case GameState.GameOver:
                    Time.timeScale = 1f;
                    break;
            }
        }
        
        public void ReturnToGameplay()
        {
            if (CurrentState is GameState.Dialog or GameState.Tutorial or GameState.Paused)
            {
                SetState(GameState.Gameplay);
            }
        }
    }

    public enum GameState
    {
        MainMenu,
        Gameplay,   // Обычная игра
        Tutorial,   // Обучение (время основной игры заморожено)
        Dialog,     // Диалог (время идёт, ходы заблокированы)
        Transition, // Переход между комнатами
        Paused,     // Меню паузы
        GameOver    // Победа/поражение
    }
}