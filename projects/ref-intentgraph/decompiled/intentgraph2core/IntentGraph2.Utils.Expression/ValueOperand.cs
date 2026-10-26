namespace IntentGraph2.Utils.Expression;

public class ValueOperand : IExpression
{
	private readonly int intValue;

	private readonly bool boolValue;

	public IExpression.Type ExpressionType { get; private init; }

	public ValueOperand(int value)
	{
		intValue = value;
		boolValue = value != 0;
		ExpressionType = IExpression.Type.Int;
	}

	public ValueOperand(bool value)
	{
		boolValue = value;
		intValue = (value ? 1 : 0);
		ExpressionType = IExpression.Type.Bool;
	}

	public int GetInt()
	{
		return intValue;
	}

	public bool GetBool()
	{
		return boolValue;
	}

	public string GetString()
	{
		return ExpressionType switch
		{
			IExpression.Type.Int => intValue.ToString(), 
			IExpression.Type.Bool => boolValue.ToString(), 
			_ => intValue.ToString(), 
		};
	}
}
