using System;
using System.CodeDom.Compiler;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace IntentGraph2.Antlr;

[GeneratedCode("ANTLR", "4.13.1")]
[CLSCompliant(false)]
public interface IExpressionListener : IParseTreeListener
{
	void EnterProg([NotNull] ExpressionParser.ProgContext context);

	void ExitProg([NotNull] ExpressionParser.ProgContext context);

	void EnterExpr([NotNull] ExpressionParser.ExprContext context);

	void ExitExpr([NotNull] ExpressionParser.ExprContext context);
}
