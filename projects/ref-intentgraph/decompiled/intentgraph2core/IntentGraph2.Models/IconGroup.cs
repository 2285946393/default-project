namespace IntentGraph2.Models;

public record IconGroup(float X = 0f, float Y = 0f, float Width = 1f, float Height = 1f, string? RelativeTo = null) : IRelativeToPosition;
