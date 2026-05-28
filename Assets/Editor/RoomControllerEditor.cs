using UnityEditor;
using UnityEngine;
using Core;
using Core.Room;

namespace Editor
{
    [CustomEditor(typeof(RoomController))]
    public class RoomControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            if (!Application.isPlaying) return;

            var room = (RoomController)target;
            
            EditorGUILayout.Space(10);

            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("🔄 Перезагрузить", GUILayout.Height(35)))
                LevelController.Instance?.RestartCurrentRoom();

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("💀 Убить всех врагов", GUILayout.Height(35)))
            {
                foreach (var enemy in room.GetComponentsInChildren<Entities.EnemyBase>())
                {
                    var health = enemy.GetComponent<Health>();
                    if (health != null && !health.IsDead)
                        health.TakeDamage(enemy.Stats.Health);
                }
            }

            GUI.backgroundColor = Color.white;
        }
    }
}