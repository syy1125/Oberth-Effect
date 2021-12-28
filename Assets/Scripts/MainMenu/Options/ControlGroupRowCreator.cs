using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.MainMenu.Options
{
public class ControlGroupRowCreator : MonoBehaviour
{
	public InputActionAsset InputActions;
	public string SimulationActionMapName;
	public GameObject ControlGroupRow;

	private void Start()
	{
		Transform parent = transform.parent;
		int siblingIndex = transform.GetSiblingIndex() + 1;

		foreach (SpecInstance<ControlGroupSpec> instance in ControlGroupDatabase.Instance.ListControlGroups())
		{
			GameObject rowObject = Instantiate(ControlGroupRow, parent);
			rowObject.transform.SetSiblingIndex(siblingIndex);
			siblingIndex++;

			ControlGroupRow row = rowObject.GetComponent<ControlGroupRow>();
			row.ControlGroupId = instance.Spec.ControlGroupId;
			row.SimulationActionMap = InputActions.FindActionMap(SimulationActionMapName, true);
			row.Label.text = StringUtils.ToTitleCase(instance.Spec.KeybindDescription);
			row.UpdateBindingDisplay();
		}

		gameObject.SetActive(false);
	}
}
}