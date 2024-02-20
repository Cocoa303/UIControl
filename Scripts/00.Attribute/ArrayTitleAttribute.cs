using UnityEngine;

//== Attribute
public class ArrayTitleAttribute : PropertyAttribute
{
    public string title;

    public ArrayTitleAttribute(string title)
    {
        this.title = title;
    }
}

#if UNITY_EDITOR
namespace UnityEditor
{
    [CustomPropertyDrawer(typeof(ArrayTitleAttribute), false)]
    public class ArrayTitleAttributeDrawer : PropertyDrawer
    {
        protected virtual ArrayTitleAttribute Attribute => attribute as ArrayTitleAttribute;
        private SerializedProperty titleProp;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            string path = property.propertyPath + "." + Attribute.title;
            titleProp = property.serializedObject.FindProperty(path);

            string newLabel = GetTitle();
            if(string.IsNullOrEmpty(newLabel)) 
            {
                newLabel = label.text;
            }
            EditorGUI.PropertyField(position,property,new GUIContent(newLabel,label.tooltip),true);
        }

        private string GetTitle()
        {
            switch ( titleProp.propertyType)
            {
                case SerializedPropertyType.Generic:
                    break;
                case SerializedPropertyType.Integer:
                    return titleProp.intValue.ToString();
                case SerializedPropertyType.Boolean:
                    return titleProp.boolValue.ToString();
                case SerializedPropertyType.Float:
                    return titleProp.floatValue.ToString();
                case SerializedPropertyType.String:
                    return titleProp.stringValue;
                case SerializedPropertyType.Color:
                    return titleProp.colorValue.ToString();
                case SerializedPropertyType.ObjectReference:
                    if (titleProp.objectReferenceValue == null) return string.Empty;
                    string nameValue = titleProp.objectReferenceValue.ToString();
                    if (nameValue.Contains("(UnityEngine.GameObject)"))
                    {
                        nameValue = nameValue.Remove(nameValue.LastIndexOf('('));
                    }
                    return nameValue;
                case SerializedPropertyType.LayerMask:
                    break;
                case SerializedPropertyType.Enum:
                    return titleProp.enumNames[titleProp.enumValueIndex];
                case SerializedPropertyType.Vector2:
                    return titleProp.vector2Value.ToString();
                case SerializedPropertyType.Vector3:
                    return titleProp.vector3Value.ToString();
                case SerializedPropertyType.Vector4:
                    return titleProp.vector4Value.ToString();
                case SerializedPropertyType.Rect:
                    break;
                case SerializedPropertyType.ArraySize:
                    break;
                case SerializedPropertyType.Character:
                    break;
                case SerializedPropertyType.AnimationCurve:
                    break;
                case SerializedPropertyType.Bounds:
                    break;
                case SerializedPropertyType.Gradient:
                    break;
                case SerializedPropertyType.Quaternion:
                    break;
                case SerializedPropertyType.ExposedReference:
                    break;
                case SerializedPropertyType.FixedBufferSize:
                    break;
                case SerializedPropertyType.Vector2Int:
                    break;
                case SerializedPropertyType.Vector3Int:
                    break;
                case SerializedPropertyType.RectInt:
                    break;
                case SerializedPropertyType.BoundsInt:
                    break;
                default:
                    break;
            }
            return "";
        }
    }
}

#endif