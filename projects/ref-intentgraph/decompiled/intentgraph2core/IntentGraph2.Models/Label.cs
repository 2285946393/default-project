namespace IntentGraph2.Models;

public record Label(float X = 0f, float Y = 0f, string Text = "", string Align = "left", int FontSize = 18, string? RelativeTo = null) : IRelativeToPosition;
