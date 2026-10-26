using System;
using IntentGraph2.Utils.Variable;

namespace IntentGraph2.Utils.Expression;

public class VariableOperand : IExpression
{
	private readonly string variableName;

	private readonly IVariableContext context;

	public IExpression.Type ExpressionType
	{
		get
		{
			Type variableType = context.GetVariableType(variableName);
			if (variableType == null)
			{
				return IExpression.Type.Unknown;
			}
			if (variableType == typeof(string))
			{
				return IExpression.Type.String;
			}
			if (variableType == typeof(bool))
			{
				return IExpression.Type.Bool;
			}
			if (variableType == typeof(int) || variableType == typeof(long) || variableType == typeof(short) || variableType == typeof(byte) || variableType == typeof(sbyte) || variableType == typeof(ushort) || variableType == typeof(uint) || variableType == typeof(ulong) || variableType == typeof(nint) || variableType == typeof(nuint) || variableType == typeof(float) || variableType == typeof(double) || variableType == typeof(decimal))
			{
				return IExpression.Type.Int;
			}
			return IExpression.Type.Unknown;
		}
	}

	public VariableOperand(string variableName, IVariableContext context)
	{
		this.variableName = variableName;
		this.context = context;
	}

	public int GetInt()
	{
		return context.GetIntVariable(variableName);
	}

	public bool GetBool()
	{
		return context.GetBoolVariable(variableName);
	}

	public string GetString()
	{
		return context.GetStringVariable(variableName);
	}
}
