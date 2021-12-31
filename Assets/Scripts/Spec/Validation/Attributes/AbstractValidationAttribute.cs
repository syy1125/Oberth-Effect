using System;

namespace Syy1125.OberthEffect.Spec.Validation.Attributes
{
public class ValidationError : Exception
{
	public ValidationError(string message) : base(message)
	{}
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public abstract class AbstractValidationAttribute : Attribute
{
	public abstract void Validate(object value);
}
}