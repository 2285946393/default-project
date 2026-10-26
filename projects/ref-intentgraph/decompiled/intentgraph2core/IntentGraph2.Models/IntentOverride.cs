namespace IntentGraph2.Models;

public record IntentOverride(string? ValueText, string? TimesText, string? VolatileValueText, MoveDetailOverride[]? Details);
