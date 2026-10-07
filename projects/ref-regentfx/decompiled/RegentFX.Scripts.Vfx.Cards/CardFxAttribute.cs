using System;

namespace RegentFX.Scripts.Vfx.Cards;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class CardFxAttribute : Attribute
{
	public Type CardType { get; }

	public CardFxAttribute(Type cardType)
	{
		CardType = cardType;
		base._002Ector();
	}
}
