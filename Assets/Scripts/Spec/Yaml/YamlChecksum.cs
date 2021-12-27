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
			.SelectMany(node => node.Value, (_, c) => Convert.ToUInt32(c))
			.Aggregate(0u, (sum, item) => sum + item);
	}
}
}