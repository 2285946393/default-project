using System;
using System.CodeDom.Compiler;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace IntentGraph2.Antlr;

[GeneratedCode("ANTLR", "4.13.1")]
[CLSCompliant(false)]
public interface IExpressionVisitor<Result> : IParseTreeVisitor<Result>
{
	Result VisitProg([NotNull] ExpressionParser.ProgContext context);

	Result VisitExpr([NotNull] ExpressionParser.ExprContext context);
}
