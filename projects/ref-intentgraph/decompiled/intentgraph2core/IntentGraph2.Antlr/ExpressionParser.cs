using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace IntentGraph2.Antlr;

[GeneratedCode("ANTLR", "4.13.1")]
[CLSCompliant(false)]
public class ExpressionParser : Parser
{
	public class ProgContext : ParserRuleContext
	{
		public override int RuleIndex => 0;

		[DebuggerNonUserCode]
		public ExprContext expr()
		{
			return GetRuleContext<ExprContext>(0);
		}

		[DebuggerNonUserCode]
		public ITerminalNode Eof()
		{
			return GetToken(-1, 0);
		}

		public ProgContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}

		[DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener)
		{
			if (listener is IExpressionListener expressionListener)
			{
				expressionListener.EnterProg(this);
			}
		}

		[DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener)
		{
			if (listener is IExpressionListener expressionListener)
			{
				expressionListener.ExitProg(this);
			}
		}

		[DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor)
		{
			if (visitor is IExpressionVisitor<TResult> expressionVisitor)
			{
				return expressionVisitor.VisitProg(this);
			}
			return visitor.VisitChildren(this);
		}
	}

	public class ExprContext : ParserRuleContext
	{
		public override int RuleIndex => 1;

		[DebuggerNonUserCode]
		public ExprContext[] expr()
		{
			return GetRuleContexts<ExprContext>();
		}

		[DebuggerNonUserCode]
		public ExprContext expr(int i)
		{
			return GetRuleContext<ExprContext>(i);
		}

		[DebuggerNonUserCode]
		public ITerminalNode BOOL()
		{
			return GetToken(13, 0);
		}

		[DebuggerNonUserCode]
		public ITerminalNode INT()
		{
			return GetToken(14, 0);
		}

		[DebuggerNonUserCode]
		public ITerminalNode VAR()
		{
			return GetToken(15, 0);
		}

		public ExprContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}

		[DebuggerNonUserCode]
		public override void EnterRule(IParseTreeListener listener)
		{
			if (listener is IExpressionListener expressionListener)
			{
				expressionListener.EnterExpr(this);
			}
		}

		[DebuggerNonUserCode]
		public override void ExitRule(IParseTreeListener listener)
		{
			if (listener is IExpressionListener expressionListener)
			{
				expressionListener.ExitExpr(this);
			}
		}

		[DebuggerNonUserCode]
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor)
		{
			if (visitor is IExpressionVisitor<TResult> expressionVisitor)
			{
				return expressionVisitor.VisitExpr(this);
			}
			return visitor.VisitChildren(this);
		}
	}

	protected static DFA[] decisionToDFA;

	protected static PredictionContextCache sharedContextCache;

	public const int T__0 = 1;

	public const int T__1 = 2;

	public const int T__2 = 3;

	public const int T__3 = 4;

	public const int T__4 = 5;

	public const int T__5 = 6;

	public const int T__6 = 7;

	public const int T__7 = 8;

	public const int T__8 = 9;

	public const int T__9 = 10;

	public const int T__10 = 11;

	public const int SPACE = 12;

	public const int BOOL = 13;

	public const int INT = 14;

	public const int VAR = 15;

	public const int RULE_prog = 0;

	public const int RULE_expr = 1;

	public static readonly string[] ruleNames;

	private static readonly string[] _LiteralNames;

	private static readonly string[] _SymbolicNames;

	public static readonly IVocabulary DefaultVocabulary;

	private static int[] _serializedATN;

	public static readonly ATN _ATN;

	[NotNull]
	public override IVocabulary Vocabulary => DefaultVocabulary;

	public override string GrammarFileName => "Expression.g4";

	public override string[] RuleNames => ruleNames;

	public override int[] SerializedAtn => _serializedATN;

	static ExpressionParser()
	{
		sharedContextCache = new PredictionContextCache();
		ruleNames = new string[2] { "prog", "expr" };
		_LiteralNames = new string[12]
		{
			null, "'!'", "'>'", "'<'", "'>='", "'<='", "'=='", "'!='", "'&&'", "'||'",
			"'('", "')'"
		};
		_SymbolicNames = new string[16]
		{
			null, null, null, null, null, null, null, null, null, null,
			null, null, "SPACE", "BOOL", "INT", "VAR"
		};
		DefaultVocabulary = new Vocabulary(_LiteralNames, _SymbolicNames);
		_serializedATN = new int[320]
		{
			4, 1, 15, 34, 2, 0, 7, 0, 2, 1,
			7, 1, 1, 0, 1, 0, 1, 0, 1, 1,
			1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
			1, 1, 1, 1, 1, 1, 1, 1, 3, 1,
			18, 8, 1, 1, 1, 1, 1, 1, 1, 1,
			1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
			1, 5, 1, 29, 8, 1, 10, 1, 12, 1,
			32, 9, 1, 1, 1, 0, 1, 2, 2, 0,
			2, 0, 1, 1, 0, 2, 7, 38, 0, 4,
			1, 0, 0, 0, 2, 17, 1, 0, 0, 0,
			4, 5, 3, 2, 1, 0, 5, 6, 5, 0,
			0, 1, 6, 1, 1, 0, 0, 0, 7, 8,
			6, 1, -1, 0, 8, 9, 5, 1, 0, 0,
			9, 18, 3, 2, 1, 8, 10, 18, 5, 13,
			0, 0, 11, 18, 5, 14, 0, 0, 12, 18,
			5, 15, 0, 0, 13, 14, 5, 10, 0, 0,
			14, 15, 3, 2, 1, 0, 15, 16, 5, 11,
			0, 0, 16, 18, 1, 0, 0, 0, 17, 7,
			1, 0, 0, 0, 17, 10, 1, 0, 0, 0,
			17, 11, 1, 0, 0, 0, 17, 12, 1, 0,
			0, 0, 17, 13, 1, 0, 0, 0, 18, 30,
			1, 0, 0, 0, 19, 20, 10, 7, 0, 0,
			20, 21, 7, 0, 0, 0, 21, 29, 3, 2,
			1, 8, 22, 23, 10, 6, 0, 0, 23, 24,
			5, 8, 0, 0, 24, 29, 3, 2, 1, 7,
			25, 26, 10, 5, 0, 0, 26, 27, 5, 9,
			0, 0, 27, 29, 3, 2, 1, 6, 28, 19,
			1, 0, 0, 0, 28, 22, 1, 0, 0, 0,
			28, 25, 1, 0, 0, 0, 29, 32, 1, 0,
			0, 0, 30, 28, 1, 0, 0, 0, 30, 31,
			1, 0, 0, 0, 31, 3, 1, 0, 0, 0,
			32, 30, 1, 0, 0, 0, 3, 17, 28, 30
		};
		_ATN = new ATNDeserializer().Deserialize(_serializedATN);
		decisionToDFA = new DFA[_ATN.NumberOfDecisions];
		for (int i = 0; i < _ATN.NumberOfDecisions; i++)
		{
			decisionToDFA[i] = new DFA(_ATN.GetDecisionState(i), i);
		}
	}

	public ExpressionParser(ITokenStream input)
		: this(input, Console.Out, Console.Error)
	{
	}

	public ExpressionParser(ITokenStream input, TextWriter output, TextWriter errorOutput)
		: base(input, output, errorOutput)
	{
		Interpreter = new ParserATNSimulator(this, _ATN, decisionToDFA, sharedContextCache);
	}

	[RuleVersion(0)]
	public ProgContext prog()
	{
		ProgContext progContext = new ProgContext(Context, base.State);
		EnterRule(progContext, 0, 0);
		try
		{
			EnterOuterAlt(progContext, 1);
			base.State = 4;
			expr(0);
			base.State = 5;
			Match(-1);
		}
		catch (RecognitionException exception)
		{
			RecognitionException e = (progContext.exception = exception);
			ErrorHandler.ReportError(this, e);
			ErrorHandler.Recover(this, e);
		}
		finally
		{
			ExitRule();
		}
		return progContext;
	}

	[RuleVersion(0)]
	public ExprContext expr()
	{
		return expr(0);
	}

	private ExprContext expr(int _p)
	{
		ParserRuleContext context = Context;
		int state = base.State;
		ExprContext exprContext = new ExprContext(Context, state);
		int state2 = 2;
		EnterRecursionRule(exprContext, 2, 1, _p);
		try
		{
			EnterOuterAlt(exprContext, 1);
			base.State = 17;
			ErrorHandler.Sync(this);
			switch (base.TokenStream.LA(1))
			{
			case 1:
				base.State = 8;
				Match(1);
				base.State = 9;
				expr(8);
				break;
			case 13:
				base.State = 10;
				Match(13);
				break;
			case 14:
				base.State = 11;
				Match(14);
				break;
			case 15:
				base.State = 12;
				Match(15);
				break;
			case 10:
				base.State = 13;
				Match(10);
				base.State = 14;
				expr(0);
				base.State = 15;
				Match(11);
				break;
			default:
				throw new NoViableAltException(this);
			}
			Context.Stop = base.TokenStream.LT(-1);
			base.State = 30;
			ErrorHandler.Sync(this);
			int num = Interpreter.AdaptivePredict(base.TokenStream, 2, Context);
			while (true)
			{
				switch (num)
				{
				case 1:
					if (ParseListeners != null)
					{
						TriggerExitRuleEvent();
					}
					base.State = 28;
					ErrorHandler.Sync(this);
					switch (Interpreter.AdaptivePredict(base.TokenStream, 1, Context))
					{
					case 1:
					{
						exprContext = new ExprContext(context, state);
						PushNewRecursionContext(exprContext, state2, 1);
						base.State = 19;
						if (!Precpred(Context, 7))
						{
							throw new FailedPredicateException(this, "Precpred(Context, 7)");
						}
						base.State = 20;
						int num2 = base.TokenStream.LA(1);
						if (((uint)num2 & 0xFFFFFFC0u) != 0 || ((1L << num2) & 0xFC) == 0L)
						{
							ErrorHandler.RecoverInline(this);
						}
						else
						{
							ErrorHandler.ReportMatch(this);
							Consume();
						}
						base.State = 21;
						expr(8);
						break;
					}
					case 2:
						exprContext = new ExprContext(context, state);
						PushNewRecursionContext(exprContext, state2, 1);
						base.State = 22;
						if (!Precpred(Context, 6))
						{
							throw new FailedPredicateException(this, "Precpred(Context, 6)");
						}
						base.State = 23;
						Match(8);
						base.State = 24;
						expr(7);
						break;
					case 3:
						exprContext = new ExprContext(context, state);
						PushNewRecursionContext(exprContext, state2, 1);
						base.State = 25;
						if (!Precpred(Context, 5))
						{
							throw new FailedPredicateException(this, "Precpred(Context, 5)");
						}
						base.State = 26;
						Match(9);
						base.State = 27;
						expr(6);
						break;
					}
					break;
				case 0:
				case 2:
					goto end_IL_032c;
				}
				base.State = 32;
				ErrorHandler.Sync(this);
				num = Interpreter.AdaptivePredict(base.TokenStream, 2, Context);
				continue;
				end_IL_032c:
				break;
			}
		}
		catch (RecognitionException exception)
		{
			RecognitionException e = (exprContext.exception = exception);
			ErrorHandler.ReportError(this, e);
			ErrorHandler.Recover(this, e);
		}
		finally
		{
			UnrollRecursionContexts(context);
		}
		return exprContext;
	}

	public override bool Sempred(RuleContext _localctx, int ruleIndex, int predIndex)
	{
		if (ruleIndex == 1)
		{
			return expr_sempred((ExprContext)_localctx, predIndex);
		}
		return true;
	}

	private bool expr_sempred(ExprContext _localctx, int predIndex)
	{
		return predIndex switch
		{
			0 => Precpred(Context, 7), 
			1 => Precpred(Context, 6), 
			2 => Precpred(Context, 5), 
			_ => true, 
		};
	}
}
