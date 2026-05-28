using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Core
{
    public static class SaveSystem
    {
        private static readonly string SaveFileName = "save.json";
        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        private static SaveData _pendingLoadData = null;

        /// <summary>
        /// Проверка наличия сохранения
        /// </summary>
        public static bool HasSave()
        {
            return File.Exists(SavePath);
        }

        /// <summary>
        /// Сохранить текущий прогресс
        /// </summary>
        public static void SaveGame()
        {
            if (LevelController.Instance == null)
            {
                Debug.LogWarning("[SaveSystem] LevelController не найден");
                return;
            }

            var data = new SaveData
            {
                currentRoomIndex = LevelController.Instance.GetRoomInfo().current,
                completedDialogues = UI.Dialogue.DialogueProgress.GetCompletedDialoguesList()
            };

            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);

            Debug.Log(
                $"<color=green>[SaveSystem]</color> Игра сохранена. Комната: {data.currentRoomIndex}, Диалогов: {data.completedDialogues.Count}");
        }

        /// <summary>
        /// Загрузить сохраненные данные
        /// </summary>
        public static void LoadGame()
        {
            if (!File.Exists(SavePath))
            {
                Debug.LogWarning("[SaveSystem] Файл сохранения не найден");
                _pendingLoadData = null;
                return;
            }

            var json = File.ReadAllText(SavePath);
            _pendingLoadData = JsonUtility.FromJson<SaveData>(json);
        }

        /// <summary>
        /// Начать новую игру (сброс данных)
        /// </summary>
        public static void StartNewGame()
        {
            _pendingLoadData = new SaveData
            {
                currentRoomIndex = 0
            };
            
            UI.Dialogue.DialogueProgress.ResetAll();
        }

        /// <summary>
        /// Применить загруженные данные к LevelController
        /// </summary>
        public static bool TryApplyPendingData(out int roomIndex)
        {
            if (_pendingLoadData != null)
            {
                roomIndex = _pendingLoadData.currentRoomIndex;
                
                UI.Dialogue.DialogueProgress.LoadFromList(_pendingLoadData.completedDialogues);
                
                _pendingLoadData = null;
                return true;
            }

            roomIndex = 0;
            return false;
        }

        /// <summary>
        /// Удалить файл сохранения
        /// </summary>
        public static void ClearSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }

            _pendingLoadData = null;
        }
    }

    [System.Serializable]
    public class SaveData
    {
        public int currentRoomIndex;
        public List<string> completedDialogues = new();
    }
}