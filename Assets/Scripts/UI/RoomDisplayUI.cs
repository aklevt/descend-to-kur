using TMPro;
using UnityEngine;
using Core;

namespace UI
{
    public class RoomDisplayUI : MonoBehaviour
    {
        [Header("UI Elements")] [SerializeField]
        private TextMeshProUGUI roomText;

        private void Start()
        {
            if (LevelController.Instance != null)
            {
                LevelController.Instance.OnRoomChanged += UpdateRoomText;


                var info = LevelController.Instance.GetRoomInfo();
                UpdateRoomText(info.current + 1, info.total);
            }
        }

        private void OnDestroy()
        {
            if (LevelController.Instance != null)
            {
                LevelController.Instance.OnRoomChanged -= UpdateRoomText;
            }
        }


        private void UpdateRoomText(int currentRoom, int totalRooms)
        {
            if (roomText == null) return;


            roomText.text = $"Уровень {currentRoom}/{totalRooms}";
        }
    }
}