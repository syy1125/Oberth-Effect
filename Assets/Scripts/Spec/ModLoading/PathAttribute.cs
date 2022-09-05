using System;

namespace Syy1125.OberthEffect.Spec.ModLoading
{
/// <summary>
/// Indicates that this class contains path fields. The mod loading process will only look for paths to resolve if the class is tagged with this attribute.
/// If the path field is nested deeply, every class along the way need to be tagged with this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ContainsPathAttribute : Attribute
{}

[AttributeUsage(AttributeTargets.Field)]
public class ResolveAbsolutePathAttribute : Attribute
{}
}