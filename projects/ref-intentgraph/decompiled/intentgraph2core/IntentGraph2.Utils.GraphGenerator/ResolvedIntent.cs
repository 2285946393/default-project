using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace IntentGraph2.Utils.GraphGenerator;

public sealed record ResolvedIntent(AbstractIntent Intent, int OriginalIntentIndex, ModelId? ModelId = null, int? Value = null, string? ValueText = null);
