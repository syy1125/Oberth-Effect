namespace Syy1125.OberthEffect.Common
{
public enum CardinalDirection
{
	Up,
	Right,
	Down,
	Left,
}

public static class CardinalDirectionUtils
{
	public static CardinalDirection Rotate(CardinalDirection direction, int rotation)
	{
		return (CardinalDirection) ((((int) direction + rotation) % 4 + 4) % 4);
	}

	public static CardinalDirection InverseRotate(CardinalDirection direction, int rotation)
	{
		return Rotate(direction, 4 - rotation);
	}
}
}