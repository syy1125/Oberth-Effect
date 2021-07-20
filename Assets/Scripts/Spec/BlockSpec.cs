using System;

namespace Syy1125.OberthEffect.Spec
{
[Serializable]
public struct BlockSpec
{
	public string BlockId;
	public string ShortName;
	public string FullName;

	public bool ShowInDesigner;
	public bool AllowErase;
	public float PreviewScale;

	public float MaxHealth;
	public float ArmorValue;
	public float IntegrityScore;
}
}