using Godot;

namespace LoadOrderManager;

internal static class LoadOrderUiInjector
{
	private const string OpenButtonName = "LoadOrderManager_OpenButton";

	private const string PanelName = "LoadOrderManager_Panel";

	public static void TryInject(object screenObject)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		Control screen = (Control)((screenObject is Control) ? screenObject : null);
		if (screen != null && GodotObject.IsInstanceValid((GodotObject)(object)screen) && ((Node)screen).FindChild("LoadOrderManager_OpenButton", true, false) == null)
		{
			Button val = new Button
			{
				Name = StringName.op_Implicit("LoadOrderManager_OpenButton"),
				Text = I18n.T("open_button"),
				TooltipText = I18n.T("open_button_tooltip"),
				FocusMode = (FocusModeEnum)2,
				MouseFilter = (MouseFilterEnum)0
			};
			((Control)val).AnchorLeft = 1f;
			((Control)val).AnchorRight = 1f;
			((Control)val).AnchorTop = 0f;
			((Control)val).AnchorBottom = 0f;
			((Control)val).OffsetLeft = -220f;
			((Control)val).OffsetRight = -20f;
			((Control)val).OffsetTop = 60f;
			((Control)val).OffsetBottom = 98f;
			((CanvasItem)val).ZIndex = 40;
			((BaseButton)val).Pressed += delegate
			{
				DebugLog.Info("Load-order button clicked.");
				EnsurePanel(screen).OpenPanel();
			};
			((Node)screen).AddChild((Node)(object)val, false, (InternalMode)0);
			DebugLog.Info("Injected load-order button into " + ((object)screen).GetType().FullName + ".");
		}
	}

	private static LoadOrderPanel EnsurePanel(Control screen)
	{
		if (((Node)screen).FindChild("LoadOrderManager_Panel", true, false) is LoadOrderPanel result)
		{
			return result;
		}
		LoadOrderPanel loadOrderPanel = new LoadOrderPanel();
		((Node)loadOrderPanel).Name = StringName.op_Implicit("LoadOrderManager_Panel");
		LoadOrderPanel loadOrderPanel2 = loadOrderPanel;
		((Node)screen).AddChild((Node)(object)loadOrderPanel2, false, (InternalMode)0);
		return loadOrderPanel2;
	}
}
