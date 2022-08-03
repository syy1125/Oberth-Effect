using Newtonsoft.Json;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.SchemaGen;
using TMPro;
using UnityEngine;

namespace Syy1125.OberthEffect
{
public class SchemaGenTest : MonoBehaviour
{
	public TMP_Text Output;

	private void Start()
	{
		var schema = SchemaGenerator.GenerateTopLevelSchema(typeof(ControlGroupSpec));
		Output.text = JsonConvert.SerializeObject(schema, Formatting.Indented);
	}
}
}