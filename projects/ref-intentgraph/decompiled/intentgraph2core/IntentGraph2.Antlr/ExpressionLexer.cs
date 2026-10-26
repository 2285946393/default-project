using System;
using System.CodeDom.Compiler;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Misc;

namespace IntentGraph2.Antlr;

[GeneratedCode("ANTLR", "4.13.1")]
[CLSCompliant(false)]
public class ExpressionLexer : Lexer
{
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

	public static string[] channelNames;

	public static string[] modeNames;

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

	public override string[] ChannelNames => channelNames;

	public override string[] ModeNames => modeNames;

	public override int[] SerializedAtn => _serializedATN;

	public ExpressionLexer(ICharStream input)
		: this(input, Console.Out, Console.Error)
	{
	}

	public ExpressionLexer(ICharStream input, TextWriter output, TextWriter errorOutput)
		: base(input, output, errorOutput)
	{
		Interpreter = new LexerATNSimulator(this, _ATN, decisionToDFA, sharedContextCache);
	}

	static ExpressionLexer()
	{
		sharedContextCache = new PredictionContextCache();
		channelNames = new string[2] { "DEFAULT_TOKEN_CHANNEL", "HIDDEN" };
		modeNames = new string[1] { "DEFAULT_MODE" };
		ruleNames = new string[15]
		{
			"T__0", "T__1", "T__2", "T__3", "T__4", "T__5", "T__6", "T__7", "T__8", "T__9",
			"T__10", "SPACE", "BOOL", "INT", "VAR"
		};
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
		_serializedATN = new int[947]
		{
			4, 0, 15, 102, 6, -1, 2, 0, 7, 0,
			2, 1, 7, 1, 2, 2, 7, 2, 2, 3,
			7, 3, 2, 4, 7, 4, 2, 5, 7, 5,
			2, 6, 7, 6, 2, 7, 7, 7, 2, 8,
			7, 8, 2, 9, 7, 9, 2, 10, 7, 10,
			2, 11, 7, 11, 2, 12, 7, 12, 2, 13,
			7, 13, 2, 14, 7, 14, 1, 0, 1, 0,
			1, 1, 1, 1, 1, 2, 1, 2, 1, 3,
			1, 3, 1, 3, 1, 4, 1, 4, 1, 4,
			1, 5, 1, 5, 1, 5, 1, 6, 1, 6,
			1, 6, 1, 7, 1, 7, 1, 7, 1, 8,
			1, 8, 1, 8, 1, 9, 1, 9, 1, 10,
			1, 10, 1, 11, 4, 11, 61, 8, 11, 11,
			11, 12, 11, 62, 1, 11, 1, 11, 1, 12,
			1, 12, 1, 12, 1, 12, 1, 12, 1, 12,
			1, 12, 1, 12, 1, 12, 3, 12, 76, 8,
			12, 1, 13, 3, 13, 79, 8, 13, 1, 13,
			4, 13, 82, 8, 13, 11, 13, 12, 13, 83,
			1, 14, 1, 14, 5, 14, 88, 8, 14, 10,
			14, 12, 14, 91, 9, 14, 1, 14, 3, 14,
			94, 8, 14, 1, 14, 1, 14, 5, 14, 98,
			8, 14, 10, 14, 12, 14, 101, 9, 14, 0,
			0, 15, 1, 1, 3, 2, 5, 3, 7, 4,
			9, 5, 11, 6, 13, 7, 15, 8, 17, 9,
			19, 10, 21, 11, 23, 12, 25, 13, 27, 14,
			29, 15, 1, 0, 4, 3, 0, 9, 10, 13,
			13, 32, 32, 1, 0, 48, 57, 3, 0, 65,
			90, 95, 95, 97, 122, 4, 0, 48, 57, 65,
			90, 95, 95, 97, 122, 108, 0, 1, 1, 0,
			0, 0, 0, 3, 1, 0, 0, 0, 0, 5,
			1, 0, 0, 0, 0, 7, 1, 0, 0, 0,
			0, 9, 1, 0, 0, 0, 0, 11, 1, 0,
			0, 0, 0, 13, 1, 0, 0, 0, 0, 15,
			1, 0, 0, 0, 0, 17, 1, 0, 0, 0,
			0, 19, 1, 0, 0, 0, 0, 21, 1, 0,
			0, 0, 0, 23, 1, 0, 0, 0, 0, 25,
			1, 0, 0, 0, 0, 27, 1, 0, 0, 0,
			0, 29, 1, 0, 0, 0, 1, 31, 1, 0,
			0, 0, 3, 33, 1, 0, 0, 0, 5, 35,
			1, 0, 0, 0, 7, 37, 1, 0, 0, 0,
			9, 40, 1, 0, 0, 0, 11, 43, 1, 0,
			0, 0, 13, 46, 1, 0, 0, 0, 15, 49,
			1, 0, 0, 0, 17, 52, 1, 0, 0, 0,
			19, 55, 1, 0, 0, 0, 21, 57, 1, 0,
			0, 0, 23, 60, 1, 0, 0, 0, 25, 75,
			1, 0, 0, 0, 27, 78, 1, 0, 0, 0,
			29, 93, 1, 0, 0, 0, 31, 32, 5, 33,
			0, 0, 32, 2, 1, 0, 0, 0, 33, 34,
			5, 62, 0, 0, 34, 4, 1, 0, 0, 0,
			35, 36, 5, 60, 0, 0, 36, 6, 1, 0,
			0, 0, 37, 38, 5, 62, 0, 0, 38, 39,
			5, 61, 0, 0, 39, 8, 1, 0, 0, 0,
			40, 41, 5, 60, 0, 0, 41, 42, 5, 61,
			0, 0, 42, 10, 1, 0, 0, 0, 43, 44,
			5, 61, 0, 0, 44, 45, 5, 61, 0, 0,
			45, 12, 1, 0, 0, 0, 46, 47, 5, 33,
			0, 0, 47, 48, 5, 61, 0, 0, 48, 14,
			1, 0, 0, 0, 49, 50, 5, 38, 0, 0,
			50, 51, 5, 38, 0, 0, 51, 16, 1, 0,
			0, 0, 52, 53, 5, 124, 0, 0, 53, 54,
			5, 124, 0, 0, 54, 18, 1, 0, 0, 0,
			55, 56, 5, 40, 0, 0, 56, 20, 1, 0,
			0, 0, 57, 58, 5, 41, 0, 0, 58, 22,
			1, 0, 0, 0, 59, 61, 7, 0, 0, 0,
			60, 59, 1, 0, 0, 0, 61, 62, 1, 0,
			0, 0, 62, 60, 1, 0, 0, 0, 62, 63,
			1, 0, 0, 0, 63, 64, 1, 0, 0, 0,
			64, 65, 6, 11, 0, 0, 65, 24, 1, 0,
			0, 0, 66, 67, 5, 116, 0, 0, 67, 68,
			5, 114, 0, 0, 68, 69, 5, 117, 0, 0,
			69, 76, 5, 101, 0, 0, 70, 71, 5, 102,
			0, 0, 71, 72, 5, 97, 0, 0, 72, 73,
			5, 108, 0, 0, 73, 74, 5, 115, 0, 0,
			74, 76, 5, 101, 0, 0, 75, 66, 1, 0,
			0, 0, 75, 70, 1, 0, 0, 0, 76, 26,
			1, 0, 0, 0, 77, 79, 5, 45, 0, 0,
			78, 77, 1, 0, 0, 0, 78, 79, 1, 0,
			0, 0, 79, 81, 1, 0, 0, 0, 80, 82,
			7, 1, 0, 0, 81, 80, 1, 0, 0, 0,
			82, 83, 1, 0, 0, 0, 83, 81, 1, 0,
			0, 0, 83, 84, 1, 0, 0, 0, 84, 28,
			1, 0, 0, 0, 85, 89, 7, 2, 0, 0,
			86, 88, 7, 3, 0, 0, 87, 86, 1, 0,
			0, 0, 88, 91, 1, 0, 0, 0, 89, 87,
			1, 0, 0, 0, 89, 90, 1, 0, 0, 0,
			90, 92, 1, 0, 0, 0, 91, 89, 1, 0,
			0, 0, 92, 94, 5, 46, 0, 0, 93, 85,
			1, 0, 0, 0, 93, 94, 1, 0, 0, 0,
			94, 95, 1, 0, 0, 0, 95, 99, 7, 2,
			0, 0, 96, 98, 7, 3, 0, 0, 97, 96,
			1, 0, 0, 0, 98, 101, 1, 0, 0, 0,
			99, 97, 1, 0, 0, 0, 99, 100, 1, 0,
			0, 0, 100, 30, 1, 0, 0, 0, 101, 99,
			1, 0, 0, 0, 8, 0, 62, 75, 78, 83,
			89, 93, 99, 1, 6, 0, 0
		};
		_ATN = new ATNDeserializer().Deserialize(_serializedATN);
		decisionToDFA = new DFA[_ATN.NumberOfDecisions];
		for (int i = 0; i < _ATN.NumberOfDecisions; i++)
		{
			decisionToDFA[i] = new DFA(_ATN.GetDecisionState(i), i);
		}
	}
}
