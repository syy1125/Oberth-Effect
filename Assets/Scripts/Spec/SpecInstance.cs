using System.Collections.Generic;

namespace Syy1125.OberthEffect.Spec
{
// Where a spec object is "pure" in the sense that it only contains information necessary to specify a particular resourc,e
// A SpecInstance is able to attach metadata to the spec.
public struct SpecInstance<T>
{
	public T Spec;
	public List<string> OverrideOrder;
}
}