using UnityEngine;
using UnityEditor;
using System.Text;

public class ExportHierarchy : UnityEditor.Editor
{
    [MenuItem("GameObject/Copy Detailed Hierarchy", false, 30)]
    private static void CopyHierarchy()
    {
        var selected = Selection.activeGameObject;
        if (selected == null) return;

        var sb = new StringBuilder();
        DumpObject(selected, sb, 0);

        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("Hierarchy copied to clipboard.");
    }

    private static void DumpObject(GameObject obj, StringBuilder sb, int indent)
    {
        var indentSpaces = new string(' ', indent * 4);
        var componentSpaces = indentSpaces + "  ";
        var propertySpaces = indentSpaces + "    ";

        sb.AppendLine($"{indentSpaces}[GO] {obj.name}");

        foreach (var comp in obj.GetComponents<Component>())
        {
            if (comp == null) continue;

            sb.AppendLine($"{componentSpaces}# {comp.GetType().Name}");

            var serializedComp = new SerializedObject(comp);
            var prop = serializedComp.GetIterator();

            if (prop.NextVisible(true))
            {
                do
                {
                    if (prop.name == "m_Script") continue;

                    var valueStr = GetPropertyValueString(prop);
                    if (valueStr != null)
                    {
                        sb.AppendLine($"{propertySpaces}- {prop.name}: {valueStr}");
                    }
                } while (prop.NextVisible(false));
            }
        }

        foreach (Transform child in obj.transform)
        {
            DumpObject(child.gameObject, sb, indent + 1);
        }
    }

    private static string GetPropertyValueString(SerializedProperty prop)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer:
                return prop.intValue.ToString();
            case SerializedPropertyType.Boolean:
                return prop.boolValue.ToString().ToLower();
            case SerializedPropertyType.Float:
                return prop.floatValue.ToString("F2");
            case SerializedPropertyType.String:
                return $"\"{prop.stringValue}\"";
            case SerializedPropertyType.Color:
                return
                    $"RGBA({prop.colorValue.r:F2}, {prop.colorValue.g:F2}, {prop.colorValue.b:F2}, {prop.colorValue.a:F2})";
            case SerializedPropertyType.ObjectReference:
                return prop.objectReferenceValue != null
                    ? $"[{prop.objectReferenceValue.GetType().Name}] {prop.objectReferenceValue.name}"
                    : "null";
            case SerializedPropertyType.Enum:
                return prop.enumDisplayNames[prop.enumValueIndex];
            case SerializedPropertyType.Vector2:
                return prop.vector2Value.ToString();
            case SerializedPropertyType.Vector3:
                return prop.vector3Value.ToString();
            case SerializedPropertyType.Rect:
                return prop.rectValue.ToString();
            case SerializedPropertyType.ArraySize:
                return $"Size: {prop.intValue}";
            case SerializedPropertyType.Character:
                return $"'{((char)prop.intValue)}'";
            case SerializedPropertyType.AnimationCurve:
                return "Curve";
            case SerializedPropertyType.Bounds:
                return prop.boundsValue.ToString();
            case SerializedPropertyType.Quaternion:
                return prop.quaternionValue.ToString();
            default:
                return prop.hasChildren ? "..." : null;
        }
    }
}