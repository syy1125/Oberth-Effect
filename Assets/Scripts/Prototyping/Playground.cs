using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Designer.Palette;
using Syy1125.OberthEffect.Lib.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.Prototyping
{
public class Playground : MonoBehaviour
{
	private static int[] RandArray()
	{
		return new[]
			{
				Random.Range(1, 10),
				Random.Range(1, 10),
				Random.Range(1, 10),
				Random.Range(1, 10),
				Random.Range(1, 10),
				Random.Range(1, 10),
				Random.Range(1, 10),
				Random.Range(1, 10),
			}
			.OrderBy(item => item)
			.ToArray();
	}

	private void Start()
	{
		Debug.Log(new BlockPaletteSearch("hello world"));
		Debug.Log(new BlockPaletteSearch("hello|world"));
		Debug.Log(new BlockPaletteSearch("#hello|world"));
		Debug.Log(new BlockPaletteSearch("hello#world"));
		Debug.Log(new BlockPaletteSearch("this #is &a # complicated | filter"));
		Debug.Log(new BlockPaletteSearch("what && happens |&| in a bad ##$ state?"));
		Debug.Log(new BlockPaletteSearch("escape\\ space or \\&op or \\#tag"));
		Debug.Log(new BlockPaletteSearch("unpaired)closing"));
		Debug.Log(new BlockPaletteSearch("unpaired(opening paren"));
	}
}
}