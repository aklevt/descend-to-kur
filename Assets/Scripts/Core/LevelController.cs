using System;
using System.Collections;
using System.Collections.Generic;
using Core.Room;
using Entities;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    /// <summary>
    /// Управляет списком комнат и переходами между ними
    /// </summary>
    public class LevelController : MonoBehaviour
    {
        public static LevelController Instance { get; private set; }

        public event Action<int, int> OnRoomChanged;

        [Header("Level Progression")] [SerializeField]
        private List<GameObject> roomPrefabs;

        [Header("Player")] [SerializeField] private GameObject playerPrefab;

        private PlayerMovement currentPlayer;
        private int currentRoomIndex = 0;
        private RoomController currentRoom;
        private bool isGameOver;

        public bool IsLevelLoaded => currentRoom != null;

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

        private void Start()
        {
            if (SaveSystem.TryApplyPendingData(out var savedRoomIndex))
            {
                Debug.Log($"<color=cyan>[LevelController]</color> Загрузка комнаты из сохранения: {savedRoomIndex}");
                currentRoomIndex = savedRoomIndex;
            }
            else
            {
                Debug.Log("<color=cyan>[LevelController]</color> Старт с комнаты 0");
                currentRoomIndex = 0;
            }

            if (roomPrefabs != null && roomPrefabs.Count > 0)
            {
                LoadRoomByIndex(currentRoomIndex);
            }
            else
            {
                Debug.LogError("[LevelController] Нет комнат для загрузки");
            }
        }

        /// <summary>
        /// Перезагрузить текущую комнату (для кнопки рестарта)
        /// </summary>
        public void RestartCurrentRoom()
        {
            if (GameStateManager.Instance?.CurrentState is GameState.Transition or GameState.GameOver)
                return;

            Debug.Log("<color=yellow>[LevelController]</color> Перезагрузка комнаты");

            if (GameStateManager.Instance?.CurrentState == GameState.Paused)
            {
                GameStateManager.Instance.SetState(GameState.Gameplay);
            }

            StopAllCoroutines();
            isGameOver = false;

            LoadRoomByIndex(currentRoomIndex);
        }

        /// <summary>
        /// Загрузить комнату по индексу
        /// </summary>
        private void LoadRoomByIndex(int index)
        {
            UIController.Instance?.SuppressPopups();
            UIController.Instance?.HideWarning();

            if (index >= roomPrefabs.Count)
            {
                Debug.Log("<color=cyan>[LevelController]</color> Все комнаты пройдены!");
                return;
            }

            UnloadCurrentRoom();


            isGameOver = false;

            var roomInstance = Instantiate(roomPrefabs[index], Vector3.zero, Quaternion.identity);
            currentRoom = roomInstance.GetComponent<RoomController>();

            if (currentRoom == null)
            {
                Debug.LogError(
                    $"[LevelController] Префаб комнаты {roomPrefabs[index].name} не содержит RoomController");
                return;
            }

            currentRoom.OnRoomCleared += HandleRoomCleared;
            currentRoom.Initialize();

            SpawnPlayer();

            if (currentRoom != null && currentPlayer != null)
            {
                currentRoom.LinkPlayerToRoom(currentPlayer);
            }

            TurnManager.Instance.ResetEnemies();
            StartCoroutine(BeginLevelNextFrame());

            if (AbilityController.Instance != null)
            {
                AbilityController.Instance.UnblockInput();
                // AbilityController.Instance.SelectAbilityByIndex(0);
            }

            Debug.Log($"<color=green>[LevelController]</color> Комната {index + 1}/{roomPrefabs.Count} загружена");
            UIController.Instance?.UnsuppressPopups();

            OnRoomChanged?.Invoke(index + 1, roomPrefabs.Count);
        }

        private IEnumerator BeginLevelNextFrame()
        {
            yield return null; // Необходимо чтобы все враги успели заспавниться, прежде чем считать подсветку

            if (currentRoom != null)
            {
                yield return currentRoom.TriggerDialoguesOfTypeSequential(DialogueTriggerType.OnRoomEnter);
            }

            TurnManager.Instance.BeginLevel();

            // if (AbilityController.Instance != null)
            // {
            //     AbilityController.Instance.UnblockInput();
            //     AbilityController.Instance.SelectAbilityByIndex(0);
            // }

            if (GameStateManager.Instance != null && !isGameOver)
            {
                GameStateManager.Instance.SetState(GameState.Gameplay);
            }

            if (AbilityController.Instance != null)
            {
                AbilityController.Instance.SelectAbilityByIndex(0);

                AbilityController.Instance.RefreshAbilityOverlay();
                AbilityController.Instance.RefreshInfoPanel();
            }

            UIController.Instance?.UnsuppressPopups();
        }

        /// <summary>
        /// Выгрузить текущую комнату
        /// </summary>
        private void UnloadCurrentRoom()
        {
            if (AbilityController.Instance != null)
                AbilityController.Instance.CancelExecution();

            if (currentPlayer != null)
                currentPlayer.StopAllCoroutines();

            if (currentRoom != null)
            {
                currentRoom.OnRoomCleared -= HandleRoomCleared;
                currentRoom.Cleanup();
                Destroy(currentRoom.gameObject);
                currentRoom = null;
            }
        }

        /// <summary>
        /// Спаун игрока в текущей комнате
        /// </summary>
        private void SpawnPlayer()
        {
            if (currentRoom?.playerSpawnPoint == null)
            {
                Debug.LogError("[LevelController] playerSpawnPoint не найден");
                return;
            }

            var spawnPos = currentRoom.playerSpawnPoint.position;

            // Игрок создается из префаба в первой комнате
            // При этом обязательно объекта игрока не должно быть на сцене
            // Иначе возникают трудноотловимые баги /ᐠ｡ꞈ｡ᐟ\
            if (PlayerMovement.Instance != null)
            {
                currentPlayer = PlayerMovement.Instance;

                var health = currentPlayer.GetComponent<Health>();
                if (health != null)
                {
                    health.StopAllCoroutines();
                    health.ResetDeathState();
                }

                Debug.Log(
                    $"<color=green>[LevelController]</color> Используется существующий игрок: {currentPlayer.name}");
                if (CameraFollow.Instance != null)
                {
                    CameraFollow.Instance.SetPlayerTarget(currentPlayer.transform);
                }

                currentPlayer.RespawnAt(spawnPos);
            }
            else if (currentPlayer == null)
            {
                if (playerPrefab == null)
                {
                    Debug.LogError("[LevelController] playerPrefab не назначен");
                    return;
                }

                var playerGO = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                currentPlayer = playerGO.GetComponent<PlayerMovement>();

                var health = currentPlayer.GetComponent<Health>();
                if (health != null)
                {
                    health.OnDeath += HandlePlayerDeath;
                }

                Debug.Log($"<color=green>[LevelController]</color> Игрок создан: {currentPlayer.name}");
            }

            if (CameraFollow.Instance != null)
            {
                CameraFollow.Instance.SetPlayerTarget(currentPlayer.transform);
            }

            currentPlayer.RespawnAt(spawnPos);

            Debug.Log(
                $"<color=green>[LevelController]</color> Игрок размещен на {spawnPos}, CurrentCell={currentPlayer.CurrentCell}");

            if (CameraFollow.Instance != null)
            {
                CameraFollow.Instance.SetPlayerTarget(currentPlayer.transform);
                CameraFollow.Instance.ResetFocus();
                CameraFollow.Instance.SnapToTarget();
            }
        }

        /// <summary>
        /// Обработка выполнения целей комнаты
        /// </summary>
        private void HandleRoomCleared()
        {
            if (isGameOver) return;
            isGameOver = true;

            AbilityController.Instance?.BlockInput();

            GameStateManager.Instance?.SetState(GameState.Transition);

            Debug.Log("<color=green>[LevelController]</color> Комната пройдена!");
            StartCoroutine(TransitionToNextRoom());
        }

        /// <summary>
        /// Переход к следующей комнате
        /// </summary>
        private IEnumerator TransitionToNextRoom()
        {
            yield return new WaitForSeconds(1.5f);

            var isLastRoom = currentRoomIndex >= roomPrefabs.Count - 1;

            if (TransitionScreenManager.Instance != null && currentRoom != null)
            {
                yield return TransitionScreenManager.Instance.ShowVictoryScreen(
                    currentRoom.VictoryMessage, 
                    isLastRoom
                );
            }

            currentRoomIndex++;

            if (currentRoomIndex < roomPrefabs.Count)
            {
                if (TransitionScreenManager.Instance != null)
                {
                    yield return TransitionScreenManager.Instance.FadeToBlack(() =>
                    {
                        LoadRoomByIndex(currentRoomIndex);
                    });

                    yield return TransitionScreenManager.Instance.FadeFromBlack();
                }
                else
                {
                    LoadRoomByIndex(currentRoomIndex);
                }
            }
            else
            {
                Debug.Log("<color=cyan>[LevelController]</color> Все уровни пройдены! Возврат в главное меню");
        
                if (TransitionScreenManager.Instance != null)
                {
                    yield return TransitionScreenManager.Instance.FadeToBlack(() =>
                    {
                        SceneManager.LoadScene("MainMenu");
                    });
                }
                else
                {
                    SceneManager.LoadScene("MainMenu");
                }
            }
        }

        /// <summary>
        /// Обработка смерти игрока
        /// </summary>
        private void HandlePlayerDeath(GameObject playerObj)
        {
            if (isGameOver) return;

            isGameOver = true;
            GameStateManager.Instance?.SetState(GameState.GameOver);

            Debug.Log("<color=red>[LevelController]</color> Игрок погиб");

            // Объект игрока больше не уничтожается целиком, чтобы менеджеры из-за него не устраивали разборок
            //
            // Отписаться от события
            // var health = playerObj.GetComponent<Health>();
            // if (health != null)
            // {
            //     health.OnDeath -= HandlePlayerDeath;
            // }
            //
            // currentPlayer = null;

            if (AbilityController.Instance != null)
            {
                AbilityController.Instance.DisableAllOverlaysAfterDeath();
            }

            TurnManager.Instance.StopAllCoroutines();
            CameraFollow.Instance?.ResetToPlayer();

            StartCoroutine(ShowDefeatAndRestart());
        }

        /// <summary>
        /// Показать экран поражения и перезапустить сцену
        /// </summary>
        private IEnumerator ShowDefeatAndRestart()
        {
            yield return new WaitForSeconds(1.5f);

            if (TransitionScreenManager.Instance != null)
            {
                yield return TransitionScreenManager.Instance.ShowDefeatScreen("ПОРАЖЕНИЕ!");
            }

            isGameOver = false;

            // Перезагрузить сцену
            // SceneManager.LoadScene(SceneManager.GetActiveScene().name);

            // Перезагрузить только комнату
            LoadRoomByIndex(currentRoomIndex);
        }

        /// <summary>
        /// Принудительный переход к следующей комнате (для редактора)
        /// </summary>
        public void ForceNextRoom()
        {
            if (currentRoomIndex >= roomPrefabs.Count - 1)
            {
                Debug.LogWarning("<color=yellow>[LevelController]</color> Это последняя комната!");
                return;
            }

            Debug.Log("<color=green>[LevelController]</color> ПРИНУДИТЕЛЬНЫЙ переход к следующей комнате");

            StopAllCoroutines();
            isGameOver = true;

            currentRoomIndex++;
            LoadRoomByIndex(currentRoomIndex);
        }

        /// <summary>
        /// Получить информацию о комнатах (для редактора)
        /// </summary>
        public (int current, int total) GetRoomInfo()
        {
            return (currentRoomIndex, roomPrefabs?.Count ?? 0);
        }
    }
}