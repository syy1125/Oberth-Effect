using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syy1125.OberthEffect.Editor.PropertyDrawers
{
public class ReadOnlyFieldAttribute : PropertyAttribute
{}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyFieldAttribute))]
public class ReadOnlyFieldDrawer : PropertyDrawer
{
	public override float GetPropertyHeight(
		SerializedProperty property,
		GUIContent label
	)
	{
		return EditorGUI.GetPropertyHeight(property, label, true);
	}

	public override void OnGUI(
		Rect position,
		SerializedProperty property,
		GUIContent label
	)
	{
		bool enabled = GUI.enabled;
		GUI.enabled = false;
		EditorGUI.PropertyField(position, property, label, true);
		GUI.enabled = enabled;
	}
}
#endif
}