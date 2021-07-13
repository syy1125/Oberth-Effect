using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Syy1125.OberthEffect.Editor.PropertyDrawers
{
public class UnityLayerAttribute : PropertyAttribute
{}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(UnityLayerAttribute))]
public class UnityLayerAttributeEditor : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		property.intValue = EditorGUI.LayerField(position, label, property.intValue);
	}
}
#endif
}