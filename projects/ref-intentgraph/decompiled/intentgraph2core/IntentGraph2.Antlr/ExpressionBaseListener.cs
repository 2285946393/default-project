using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace IntentGraph2.Antlr;

[GeneratedCode("ANTLR", "4.13.1")]
[DebuggerNonUserCode]
[CLSCompliant(false)]
public class ExpressionBaseListener : IExpressionListener, IParseTreeListener
{
	public virtual void EnterProg([NotNull] ExpressionParser.ProgContext context)
	{
	}

	public virtual void ExitProg([NotNull] ExpressionParser.ProgContext context)
	{
	}

	public virtual void EnterExpr([NotNull] ExpressionParser.ExprContext context)
	{
	}

	public virtual void ExitExpr([NotNull] ExpressionParser.ExprContext context)
	{
	}

	public virtual void EnterEveryRule([NotNull] ParserRuleContext context)
	{
	}

	public virtual void ExitEveryRule([NotNull] ParserRuleContext context)
	{
	}

	public virtual void VisitTerminal([NotNull] ITerminalNode node)
	{
	}

	public virtual void VisitErrorNode([NotNull] IErrorNode node)
	{
	}
}
