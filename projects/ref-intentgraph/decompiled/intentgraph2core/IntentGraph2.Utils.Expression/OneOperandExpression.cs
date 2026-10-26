namespace IntentGraph2.Utils.Expression;

public class OneOperandExpression : IExpression
{
	private readonly IExpression.Operator compareOperator;

	private readonly IExpression operandA;

	public IExpression.Type ExpressionType => IExpression.Type.Bool;

	public OneOperandExpression(IExpression.Operator compareOperator, IExpression operandA)
	{
		this.compareOperator = compareOperator;
		this.operandA = operandA;
	}

	private bool Check()
	{
		if (compareOperator == IExpression.Operator.NOT)
		{
			return !operandA.GetBool();
		}
		return false;
	}

	public int GetInt()
	{
		return Check() ? 1 : 0;
	}

	public bool GetBool()
	{
		return Check();
	}

	public string GetString()
	{
		return Check().ToString();
	}
}
