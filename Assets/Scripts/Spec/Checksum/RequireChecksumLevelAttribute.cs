using System;

namespace Syy1125.OberthEffect.Spec.Checksum
{
public enum ChecksumLevel
{
	Basic, // Only fields relevant to gameplay logic 
	Strict, // Fields relevant to gameplay logic and display
	Everything // Everything, including fields that may not matter
}

[AttributeUsage(AttributeTargets.Field)]
public class RequireChecksumLevelAttribute : Attribute
{
	public RequireChecksumLevelAttribute(ChecksumLevel level)
	{}
}
}