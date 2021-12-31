namespace Syy1125.OberthEffect.Lib.Math
{
// Single variable expression
public interface IExpression
{
	float Evaluate(float x);
	bool IsDifferentiable();
	IExpression GetDerivative();
}
}