using System;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Syy1125.OberthEffect.Spec.Yaml
{
public static class YamlChecksum
{
	public static uint GetChecksum(YamlDocument document)
	{
		return document.AllNodes
			.OfType<YamlScalarNode>()
			.Select(node => node.Value.Aggregate(0u, (sum, item) => sum + Convert.ToUInt32(item)))
			.Aggregate(0u, (sum, item) => sum + item);
	}
}
}