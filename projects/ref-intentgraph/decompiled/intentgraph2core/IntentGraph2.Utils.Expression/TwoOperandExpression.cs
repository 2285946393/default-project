namespace IntentGraph2.Utils.Expression;

public class TwoOperandExpression : IExpression
{
	private readonly IExpression.Operator compareOperator;

	private readonly IExpression operandA;

	private readonly IExpression operandB;

	public IExpression.Type ExpressionType => IExpression.Type.Bool;

	public TwoOperandExpression(IExpression operandA, IExpression.Operator compareOperator, IExpression operandB)
	{
		this.compareOperator = compareOperator;
		this.operandA = operandA;
		this.operandB = operandB;
	}

	private bool Check()
	{
		return compareOperator switch
		{
			IExpression.Operator.EQ => operandA.GetInt() == operandB.GetInt(), 
			IExpression.Operator.LT => operandA.GetInt() < operandB.GetInt(), 
			IExpression.Operator.GT => operandA.GetInt() > operandB.GetInt(), 
			IExpression.Operator.LE => operandA.GetInt() <= operandB.GetInt(), 
			IExpression.Operator.GE => operandA.GetInt() >= operandB.GetInt(), 
			IExpression.Operator.NE => operandA.GetInt() != operandB.GetInt(), 
			IExpression.Operator.AND => operandA.GetBool() && operandB.GetBool(), 
			IExpression.Operator.OR => operandA.GetBool() || operandB.GetBool(), 
			_ => false, 
		};
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
