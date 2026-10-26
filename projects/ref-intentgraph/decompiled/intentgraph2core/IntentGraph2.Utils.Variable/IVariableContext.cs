using System;

namespace IntentGraph2.Utils.Variable;

public interface IVariableContext
{
	int GetIntVariable(string variableName);

	bool GetBoolVariable(string variableName);

	string GetStringVariable(string variableName);

	Type? GetVariableType(string variableName);
}
