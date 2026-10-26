using System;
using System.Collections.Generic;
using System.Linq;
using IntentGraph2.Utils;
using IntentGraph2.Utils.Expression;
using IntentGraph2.Utils.Variable;
using MegaCrit.Sts2.Core.Models;

namespace IntentGraph2.Models;

public class IntentDefinitionList : List<IntentDefinition>
{
	public IntentDefinition? FindFirstMatchIntentDefinition(VariableContext expressionContext)
	{
		foreach (IntentDefinition item in Enumerable.Reverse(this))
		{
			try
			{
				IExpression? expression = IExpression.Parse(item.Condition, expressionContext);
				if (expression != null && expression.GetBool())
				{
					return item;
				}
			}
			catch (Exception ex)
			{
				IgLogger.Warn($"Error parsing condition '{item.Condition}' for monster '{((AbstractModel)expressionContext.Monster).Id}': {ex.Message}");
			}
		}
		return null;
	}
}
