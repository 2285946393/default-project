using Godot;
using IntentGraph2.Scenes;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;

namespace IntentGraph2.Utils;

public static class IntentGraphEditorHost
{
	private const string EditorScenePath = "res://intentgraph2/scenes/intent_graph_editor.tscn";

	private static NIntentGraphEditor? currentEditor;

	public static bool TryOpenEditor(MonsterModel monster, string monsterDisplayName, out string message)
	{
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		if (IsEditorAlive(currentEditor))
		{
			if (currentEditor.HasUnsavedChanges)
			{
				message = "An intent editor is already open with unsaved changes. Close it before opening another editor.";
				return false;
			}
			((Node)currentEditor).QueueFree();
			currentEditor = null;
		}
		Node val = ResolveEditorParent();
		if (val == null)
		{
			message = "Failed to find a UI parent for the intent editor.";
			return false;
		}
		PackedScene val2 = ResourceLoader.Load<PackedScene>("res://intentgraph2/scenes/intent_graph_editor.tscn", (string)null, (CacheMode)1);
		if (val2 == null)
		{
			message = "Failed to load editor scene 'res://intentgraph2/scenes/intent_graph_editor.tscn'.";
			return false;
		}
		NIntentGraphEditor editor = val2.Instantiate<NIntentGraphEditor>((GenEditState)0);
		val.AddChild((Node)(object)editor, false, (InternalMode)0);
		((CanvasItem)editor).TopLevel = true;
		((Control)editor).Position = Vector2.Zero;
		NIntentGraphEditor nIntentGraphEditor = editor;
		Control val3 = (Control)(object)((val is Control) ? val : null);
		Vector2 size;
		if (val3 == null)
		{
			Rect2 viewportRect = ((CanvasItem)editor).GetViewportRect();
			size = ((Rect2)(ref viewportRect)).Size;
		}
		else
		{
			size = val3.Size;
		}
		((Control)nIntentGraphEditor).Size = size;
		((CanvasItem)editor).ZIndex = 1000;
		editor.Initialize(monster, monsterDisplayName);
		((Node)editor).TreeExited += delegate
		{
			if (currentEditor == editor)
			{
				currentEditor = null;
			}
		};
		currentEditor = editor;
		message = "Opened intent editor for " + monsterDisplayName + ".";
		return true;
	}

	public static void CloseEditor(NIntentGraphEditor editor)
	{
		if (currentEditor == editor)
		{
			currentEditor = null;
		}
		if (GodotObject.IsInstanceValid((GodotObject)(object)editor))
		{
			((Node)editor).QueueFree();
		}
	}

	private static Node? ResolveEditorParent()
	{
		if (NGame.Instance != null)
		{
			return (Node?)(object)NGame.Instance;
		}
		MainLoop mainLoop = Engine.GetMainLoop();
		MainLoop obj = ((mainLoop is SceneTree) ? mainLoop : null);
		if (obj == null)
		{
			return null;
		}
		return (Node?)(object)((SceneTree)obj).Root;
	}

	private static bool IsEditorAlive(NIntentGraphEditor? editor)
	{
		if (editor != null)
		{
			return GodotObject.IsInstanceValid((GodotObject)(object)editor);
		}
		return false;
	}
}
