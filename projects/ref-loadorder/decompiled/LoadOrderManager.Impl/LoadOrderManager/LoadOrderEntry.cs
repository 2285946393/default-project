using System.Collections.Generic;

namespace LoadOrderManager;

internal sealed class LoadOrderEntry
{
	public string Id { get; }

	public string Name { get; }

	public int Source { get; }

	public bool IsEnabled { get; set; }

	public bool IsNew { get; set; }

	public List<string> Dependencies { get; } = new List<string>();


	public string SourceText => Source switch
	{
		1 => I18n.T("source_mods"), 
		2 => I18n.T("source_workshop"), 
		_ => I18n.T("source_unknown"), 
	};

	public string Key => BuildKey(Id, Source);

	public LoadOrderEntry(string id, string name, int source, bool isEnabled)
	{
		Id = id;
		Name = (string.IsNullOrWhiteSpace(name) ? id : name);
		Source = source;
		IsEnabled = isEnabled;
	}

	public static string BuildKey(string id, int source)
	{
		return $"{id}::{source}";
	}
}
