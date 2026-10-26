using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace IntentGraph2.Antlr;

[GeneratedCode("ANTLR", "4.13.1")]
[DebuggerNonUserCode]
[CLSCompliant(false)]
public class ExpressionBaseVisitor<Result> : AbstractParseTreeVisitor<Result>, IExpressionVisitor<Result>, IParseTreeVisitor<Result>
{
	public virtual Result VisitProg([NotNull] ExpressionParser.ProgContext context)
	{
		return VisitChildren(context);
	}

	public virtual Result VisitExpr([NotNull] ExpressionParser.ExprContext context)
	{
		return VisitChildren(context);
	}
}
