#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Intro.Editor
{
    [CustomEditor(typeof(IntroSequence))]
    public class IntroSequenceEditor : UnityEditor.Editor
    {
        private SerializedProperty stepsProp;
        private SerializedProperty typeSpeedProp;
        private SerializedProperty canSkipProp;
        private SerializedProperty sceneToLoadProp;

        private void OnEnable()
        {
            stepsProp = serializedObject.FindProperty("steps");
            typeSpeedProp = serializedObject.FindProperty("narrativeTypeSpeed");
            canSkipProp = serializedObject.FindProperty("canSkip");
            sceneToLoadProp = serializedObject.FindProperty("sceneToLoad");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Intro Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(typeSpeedProp);
            EditorGUILayout.PropertyField(canSkipProp);
            EditorGUILayout.PropertyField(sceneToLoadProp);

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Steps", EditorStyles.boldLabel);

            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("➕ Narrative", GUILayout.Height(30)))
                AddStep(StepType.Narrative);
            if (GUILayout.Button("➕ Dialogue", GUILayout.Height(30)))
                AddStep(StepType.Dialogue);
            if (GUILayout.Button("➕ Image", GUILayout.Height(30)))
                AddStep(StepType.Image);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            
            for (var i = 0; i < stepsProp.arraySize; i++)
            {
                DrawStep(i);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStep(int index)
        {
            var stepProp = stepsProp.GetArrayElementAtIndex(index);
            var typeProp = stepProp.FindPropertyRelative("type");
            var narrativeTextProp = stepProp.FindPropertyRelative("narrativeText");
            var dialogueProp = stepProp.FindPropertyRelative("dialogue");
            var imageProp = stepProp.FindPropertyRelative("image");
            var captionProp = stepProp.FindPropertyRelative("imageCaption");
            var delayProp = stepProp.FindPropertyRelative("delayAfter");

            var stepType = (StepType)typeProp.enumValueIndex;

            
            GUI.backgroundColor = GetStepColor(stepType);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            
            EditorGUILayout.BeginHorizontal();
            var icon = GetStepIcon(stepType);
            EditorGUILayout.LabelField($"{icon} Step {index + 1}: {stepType}", EditorStyles.boldLabel);

            
            if (GUILayout.Button("▲", GUILayout.Width(30)) && index > 0)
            {
                stepsProp.MoveArrayElement(index, index - 1);
                return;
            }
            if (GUILayout.Button("▼", GUILayout.Width(30)) && index < stepsProp.arraySize - 1)
            {
                stepsProp.MoveArrayElement(index, index + 1);
                return;
            }
            if (GUILayout.Button("❌", GUILayout.Width(30)))
            {
                stepsProp.DeleteArrayElementAtIndex(index);
                return;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(typeProp);

            
            switch (stepType)
            {
                case StepType.Narrative:
                    EditorGUILayout.PropertyField(narrativeTextProp, new GUIContent("Text"), GUILayout.Height(100));
                    break;

                case StepType.Dialogue:
                    EditorGUILayout.PropertyField(dialogueProp, new GUIContent("Dialogue Data"));
                    EditorGUILayout.HelpBox("Создайте DialogueData: ПКМ → Create → Dialogue → Dialogue Data", MessageType.Info);
                    break;

                case StepType.Image:
                    EditorGUILayout.PropertyField(imageProp, new GUIContent("Image"));
                    EditorGUILayout.PropertyField(captionProp, new GUIContent("Caption (Optional)"), GUILayout.Height(50));
                    break;
            }

            EditorGUILayout.PropertyField(delayProp, new GUIContent("Delay After"));

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void AddStep(StepType type)
        {
            var newIndex = stepsProp.arraySize;
            stepsProp.InsertArrayElementAtIndex(newIndex);

            var newStep = stepsProp.GetArrayElementAtIndex(newIndex);
            newStep.FindPropertyRelative("type").enumValueIndex = (int)type;
            newStep.FindPropertyRelative("narrativeText").stringValue = "";
            newStep.FindPropertyRelative("dialogue").objectReferenceValue = null;
            newStep.FindPropertyRelative("image").objectReferenceValue = null;
            newStep.FindPropertyRelative("imageCaption").stringValue = "";
            newStep.FindPropertyRelative("delayAfter").floatValue = 0.5f;
        }

        private Color GetStepColor(StepType type)
        {
            return type switch
            {
                StepType.Narrative => new Color(0.9f, 0.9f, 1f, 0.3f),
                StepType.Dialogue => new Color(0.9f, 1f, 0.9f, 0.3f),
                StepType.Image => new Color(1f, 0.95f, 0.8f, 0.3f),
                _ => Color.white
            };
        }

        private string GetStepIcon(StepType type)
        {
            return type switch
            {
                StepType.Narrative => "📜",
                StepType.Dialogue => "💬",
                StepType.Image => "🖼️",
                _ => "•"
            };
        }
    }
}
#endif