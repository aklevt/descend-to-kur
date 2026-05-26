using System.Collections.Generic;

namespace UI.Dialogue
{
    /// <summary>
    /// Сохранение прочитанных диалогов во время игры
    /// </summary>
    public static class DialogueProgress
    {
        private static HashSet<string> _completedDialogues = new HashSet<string>();

        public static bool IsCompleted(string dialogueID)
        {
            return _completedDialogues.Contains(dialogueID);
        }

        public static void MarkCompleted(string dialogueID)
        {
            _completedDialogues.Add(dialogueID);
        }

        /// <summary>
        /// Возвращает копию прогресса в виде списка для сериализации в JSON
        /// </summary>
        public static List<string> GetCompletedDialoguesList()
        {
            return new List<string>(_completedDialogues);
        }

        /// <summary>
        /// Заполняет прогресс из сохраненного списка
        /// </summary>
        public static void LoadFromList(List<string> loadedList)
        {
            _completedDialogues = loadedList != null
                ? new HashSet<string>(loadedList)
                : new HashSet<string>();
        }

        /// <summary>
        /// Сбросить все диалоги 
        /// </summary>
        public static void ResetAll()
        {
            _completedDialogues.Clear();
        }
    }
}