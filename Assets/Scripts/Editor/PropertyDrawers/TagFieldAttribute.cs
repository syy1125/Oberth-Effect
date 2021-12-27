using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syy1125.OberthEffect.Editor.PropertyDrawers
{
public class TagFieldAttribute : PropertyAttribute
{}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(TagFieldAttribute))]
public class TagFieldAttributeEditor : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
	}
}
#endif
}