using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using IntentGraph2.Models;
using IntentGraph2.Utils.Expression;
using IntentGraph2.Utils.Variable;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace IntentGraph2.Utils.GraphGenerator;

internal class IntentGraphLocalizer
{
	public delegate string? OnFoundVariableDelegate(string variableName, string? variableType);

	private readonly IReadOnlyDictionary<string, string>? overwriteIntentStrings;

	private readonly VariableContext variableContext;

	private readonly IntentDefinition? intentDefinition;

	private static readonly Regex PlaceholderFinder = new Regex("{{|{([^}]+)}", RegexOptions.Compiled);

	public IntentGraphLocalizer(IReadOnlyDictionary<string, string>? overwriteIntentStrings, VariableContext variableContext, IntentDefinition? intentDefinition)
	{
		this.overwriteIntentStrings = overwriteIntentStrings;
		this.variableContext = variableContext;
		this.intentDefinition = intentDefinition;
	}

	public bool TryGet(string key, [NotNullWhen(true)] out string? value)
	{
		if (overwriteIntentStrings != null && overwriteIntentStrings.TryGetValue(key, out value))
		{
			return true;
		}
		return IntentGraphMod.IntentGraphStrings.TryGetValue(key, out value);
	}

	public string GetOrElse(string key, string fallbackValue)
	{
		if (!TryGet(key, out string value))
		{
			return fallbackValue;
		}
		return value;
	}

	public string FormatWithVariables(string text, OnFoundVariableDelegate? onFoundVariable = null)
	{
		OnFoundVariableDelegate onFoundVariable2 = onFoundVariable;
		return PlaceholderFinder.Replace(text, delegate(Match match)
		{
			if (match.Value == "{{")
			{
				return "{";
			}
			string[] array = match.Groups[1].Value.Split(':', 2);
			string text2 = array[0];
			string text3 = ((array.Length > 1) ? array[1] : null);
			if (onFoundVariable2 != null)
			{
				string text4 = onFoundVariable2(text2, text3);
				if (text4 != null)
				{
					return text4;
				}
			}
			IExpression expression = IExpression.Parse(text2, variableContext);
			if (expression == null)
			{
				return string.Empty;
			}
			if (text3 == "int")
			{
				return expression.GetInt().ToString();
			}
			return (text3 == "bool") ? expression.GetBool().ToString() : expression.GetString();
		});
	}

	public string? GetMoveName(MonsterModel monster, string moveId)
	{
		string moveName = GetMoveName(((AbstractModel)monster).Id.Entry, moveId);
		if (moveName == null && intentDefinition?.AlternativeMonsterId != null)
		{
			return GetMoveName(intentDefinition.AlternativeMonsterId, moveId);
		}
		return moveName;
	}

	private string? GetMoveName(string monsterName, string moveId)
	{
		LocTable table = LocManager.Instance.GetTable("monsters");
		if (TryGetTitle(table, monsterName, moveId, out string title))
		{
			return title;
		}
		bool flag = true;
		while (flag)
		{
			flag = false;
			if (moveId.EndsWith("_MOVE"))
			{
				moveId = moveId.Substring(0, moveId.Length - "_MOVE".Length);
				flag = true;
			}
			int num = moveId.Length - 1;
			while (num >= 0 && ((moveId[num] >= '0' && moveId[num] <= '9') || moveId[num] == '_'))
			{
				num--;
			}
			if (num < moveId.Length - 1)
			{
				moveId = moveId.Substring(0, num + 1);
				flag = true;
			}
		}
		if (!TryGetTitle(table, monsterName, moveId, out title) && moveId.Contains('_'))
		{
			int num2 = moveId.IndexOf('_');
			TryGetTitle(table, monsterName, moveId, out title, num2 + 1);
		}
		if (title == null && !TryGetTitle(monsterName, moveId, out title) && moveId.Contains('_'))
		{
			int num3 = moveId.IndexOf('_');
			TryGetTitle(monsterName, moveId, out title, num3 + 1);
		}
		return title;
	}

	private bool TryGetTitle(LocTable table, string monsterName, string moveId, out string? title, int startIndex = 0, int? endIndex = null)
	{
		title = null;
		int num = endIndex ?? moveId.Length;
		if (startIndex == -1 || num == -1 || num <= startIndex)
		{
			return false;
		}
		string text = monsterName + ".moves." + moveId.Substring(startIndex, num - startIndex) + ".title";
		if (table.HasEntry(text))
		{
			title = table.GetRawText(text);
			return true;
		}
		return false;
	}

	private bool TryGetTitle(string monsterName, string moveId, out string? title, int startIndex = 0, int? endIndex = null)
	{
		title = null;
		int num = endIndex ?? moveId.Length;
		if (startIndex == -1 || num == -1 || num <= startIndex)
		{
			return false;
		}
		string key = monsterName + ".moves." + moveId.Substring(startIndex, num - startIndex) + ".title";
		if (TryGet(key, out string value))
		{
			title = value;
			return true;
		}
		return false;
	}
}
