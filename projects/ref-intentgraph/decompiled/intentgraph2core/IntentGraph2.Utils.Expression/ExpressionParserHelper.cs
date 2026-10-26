using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using IntentGraph2.Antlr;
using IntentGraph2.Utils.Variable;

namespace IntentGraph2.Utils.Expression;

public static class ExpressionParserHelper
{
	private static readonly Dictionary<string, IExpression.Operator> OperatorMap = new Dictionary<string, IExpression.Operator>
	{
		[">"] = IExpression.Operator.GT,
		["<"] = IExpression.Operator.LT,
		["=="] = IExpression.Operator.EQ,
		[">="] = IExpression.Operator.GE,
		["<="] = IExpression.Operator.LE,
		["!="] = IExpression.Operator.NE,
		["&&"] = IExpression.Operator.AND,
		["||"] = IExpression.Operator.OR
	};

	public static IExpression? Parse(string expression, IVariableContext expressionContext)
	{
		return Expr(new ExpressionParser(new CommonTokenStream(new ExpressionLexer(CharStreams.fromString(expression)))).prog().expr(), expressionContext);
	}

	private static IExpression? Expr(IParseTree tree, IVariableContext expressionContext)
	{
		if (tree.ChildCount == 1)
		{
			if (tree.GetChild(0) is ITerminalNode { Symbol: var symbol })
			{
				if (symbol.Type == 15)
				{
					return new VariableOperand(symbol.Text, expressionContext);
				}
				if (symbol.Type == 14)
				{
					return new ValueOperand(int.Parse(symbol.Text));
				}
				if (symbol.Type == 13)
				{
					return new ValueOperand(bool.Parse(symbol.Text));
				}
			}
		}
		else if (tree.ChildCount == 2)
		{
			if (tree.GetChild(0) is ITerminalNode terminalNode2 && terminalNode2.Symbol.Text == "!")
			{
				IExpression expression = Expr(tree.GetChild(1), expressionContext);
				if (expression != null)
				{
					return new OneOperandExpression(IExpression.Operator.NOT, expression);
				}
			}
		}
		else if (tree.ChildCount == 3)
		{
			IParseTree child = tree.GetChild(0);
			IParseTree child2 = tree.GetChild(1);
			IExpression.Operator value;
			if (child is ITerminalNode terminalNode3)
			{
				if (terminalNode3.Symbol.Text == "(")
				{
					return Expr(tree.GetChild(1), expressionContext);
				}
			}
			else if (child2 is ITerminalNode terminalNode4 && OperatorMap.TryGetValue(terminalNode4.Symbol.Text, out value))
			{
				IExpression expression2 = Expr(child, expressionContext);
				IExpression expression3 = Expr(tree.GetChild(2), expressionContext);
				if (expression2 != null && expression3 != null)
				{
					return new TwoOperandExpression(expression2, value, expression3);
				}
			}
		}
		return null;
	}
}
