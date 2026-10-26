using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using IntentGraph2.Models;
using IntentGraph2.Utils;
using IntentGraph2.Utils.Expression;
using IntentGraph2.Utils.GraphGenerator;
using IntentGraph2.Utils.Variable;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace IntentGraph2.Scenes;

[ScriptPath("res://intentgraph2/src/Scenes/NIntentGraphEditor.cs")]
public class NIntentGraphEditor : Control
{
	private enum JsonEditorKind
	{
		SecondaryInitialStates,
		StateMachine,
		MoveReplacements,
		GraphPatch
	}

	private enum PendingAction
	{
		None,
		Close,
		Reload
	}

	private readonly record struct CompletionItem(string MatchText, string DisplayText, string InsertText, CodeCompletionKind KindValue)
	{
		[CompilerGenerated]
		private bool PrintMembers(StringBuilder builder)
		{
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			builder.Append("MatchText = ");
			builder.Append((object?)MatchText);
			builder.Append(", DisplayText = ");
			builder.Append((object?)DisplayText);
			builder.Append(", InsertText = ");
			builder.Append((object?)InsertText);
			builder.Append(", KindValue = ");
			CodeCompletionKind kindValue = KindValue;
			builder.Append(((object)(CodeCompletionKind)(ref kindValue)).ToString());
			return true;
		}

		[CompilerGenerated]
		public void Deconstruct(out string MatchText, out string DisplayText, out string InsertText, out CodeCompletionKind KindValue)
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Expected I8, but got Unknown
			MatchText = this.MatchText;
			DisplayText = this.DisplayText;
			InsertText = this.InsertText;
			KindValue = (CodeCompletionKind)(long)this.KindValue;
		}
	}

	public class MethodName : MethodName
	{
		public static readonly StringName LocalizeText = StringName.op_Implicit("LocalizeText");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName _UnhandledInput = StringName.op_Implicit("_UnhandledInput");

		public static readonly StringName ApplyViewportLayout = StringName.op_Implicit("ApplyViewportLayout");

		public static readonly StringName LoadDefinitionsFromDiskOrRuntime = StringName.op_Implicit("LoadDefinitionsFromDiskOrRuntime");

		public static readonly StringName ApplyLocalization = StringName.op_Implicit("ApplyLocalization");

		public static readonly StringName SetTabTitle = StringName.op_Implicit("SetTabTitle");

		public static readonly StringName RefreshUiFromState = StringName.op_Implicit("RefreshUiFromState");

		public static readonly StringName RefreshVariantList = StringName.op_Implicit("RefreshVariantList");

		public static readonly StringName RefreshStateIdList = StringName.op_Implicit("RefreshStateIdList");

		public static readonly StringName LoadSelectedDefinitionIntoEditors = StringName.op_Implicit("LoadSelectedDefinitionIntoEditors");

		public static readonly StringName LoadIntentStringsIntoEditor = StringName.op_Implicit("LoadIntentStringsIntoEditor");

		public static readonly StringName UpdateActionState = StringName.op_Implicit("UpdateActionState");

		public static readonly StringName SetEditorsEnabled = StringName.op_Implicit("SetEditorsEnabled");

		public static readonly StringName OnEditorFieldsChanged = StringName.op_Implicit("OnEditorFieldsChanged");

		public static readonly StringName OnIntentStringsChanged = StringName.op_Implicit("OnIntentStringsChanged");

		public static readonly StringName ConfigureCodeEditor = StringName.op_Implicit("ConfigureCodeEditor");

		public static readonly StringName OnCodeEditorTextChanged = StringName.op_Implicit("OnCodeEditorTextChanged");

		public static readonly StringName OnCodeCompletionRequested = StringName.op_Implicit("OnCodeCompletionRequested");

		public static readonly StringName RegisterEditableInput = StringName.op_Implicit("RegisterEditableInput");

		public static readonly StringName OnVariantSelected = StringName.op_Implicit("OnVariantSelected");

		public static readonly StringName OnAddVariantPressed = StringName.op_Implicit("OnAddVariantPressed");

		public static readonly StringName OnStateIdClicked = StringName.op_Implicit("OnStateIdClicked");

		public static readonly StringName OnDuplicateVariantPressed = StringName.op_Implicit("OnDuplicateVariantPressed");

		public static readonly StringName OnDeleteVariantPressed = StringName.op_Implicit("OnDeleteVariantPressed");

		public static readonly StringName OnMoveVariantUpPressed = StringName.op_Implicit("OnMoveVariantUpPressed");

		public static readonly StringName OnMoveVariantDownPressed = StringName.op_Implicit("OnMoveVariantDownPressed");

		public static readonly StringName MoveSelectedVariant = StringName.op_Implicit("MoveSelectedVariant");

		public static readonly StringName OnSavePressed = StringName.op_Implicit("OnSavePressed");

		public static readonly StringName OnReloadPressed = StringName.op_Implicit("OnReloadPressed");

		public static readonly StringName OnClosePressed = StringName.op_Implicit("OnClosePressed");

		public static readonly StringName RequestClose = StringName.op_Implicit("RequestClose");

		public static readonly StringName OnUnsavedChangesConfirmed = StringName.op_Implicit("OnUnsavedChangesConfirmed");

		public static readonly StringName ReloadDefinitions = StringName.op_Implicit("ReloadDefinitions");

		public static readonly StringName CloseEditor = StringName.op_Implicit("CloseEditor");

		public static readonly StringName RefreshPreview = StringName.op_Implicit("RefreshPreview");

		public static readonly StringName UpdateConditionStatus = StringName.op_Implicit("UpdateConditionStatus");

		public static readonly StringName UpdateUpToDateConditionStatus = StringName.op_Implicit("UpdateUpToDateConditionStatus");

		public static readonly StringName UpdateHeaderStatus = StringName.op_Implicit("UpdateHeaderStatus");

		public static readonly StringName UpdateIntentStringsStatus = StringName.op_Implicit("UpdateIntentStringsStatus");

		public static readonly StringName InsertStateIdIntoActiveInput = StringName.op_Implicit("InsertStateIdIntoActiveInput");

		public static readonly StringName GetCompletionPrefix = StringName.op_Implicit("GetCompletionPrefix");

		public static readonly StringName IsCompletionCharacter = StringName.op_Implicit("IsCompletionCharacter");

		public static readonly StringName IsCaretInsideString = StringName.op_Implicit("IsCaretInsideString");

		public static readonly StringName ShowMessage = StringName.op_Implicit("ShowMessage");

		public static readonly StringName NormalizeSecondaryInitialStates = StringName.op_Implicit("NormalizeSecondaryInitialStates");

		public static readonly StringName NormalizeConditionText = StringName.op_Implicit("NormalizeConditionText");

		public static readonly StringName FormatPositionComponent = StringName.op_Implicit("FormatPositionComponent");

		public static readonly StringName NormalizeOptionalExpressionText = StringName.op_Implicit("NormalizeOptionalExpressionText");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName HasUnsavedChanges = StringName.op_Implicit("HasUnsavedChanges");

		public static readonly StringName monsterNameLabel = StringName.op_Implicit("monsterNameLabel");

		public static readonly StringName monsterModelLabel = StringName.op_Implicit("monsterModelLabel");

		public static readonly StringName headerStatusLabel = StringName.op_Implicit("headerStatusLabel");

		public static readonly StringName variantList = StringName.op_Implicit("variantList");

		public static readonly StringName stateIdList = StringName.op_Implicit("stateIdList");

		public static readonly StringName addVariantButton = StringName.op_Implicit("addVariantButton");

		public static readonly StringName duplicateVariantButton = StringName.op_Implicit("duplicateVariantButton");

		public static readonly StringName deleteVariantButton = StringName.op_Implicit("deleteVariantButton");

		public static readonly StringName moveVariantUpButton = StringName.op_Implicit("moveVariantUpButton");

		public static readonly StringName moveVariantDownButton = StringName.op_Implicit("moveVariantDownButton");

		public static readonly StringName saveButton = StringName.op_Implicit("saveButton");

		public static readonly StringName reloadButton = StringName.op_Implicit("reloadButton");

		public static readonly StringName closeButton = StringName.op_Implicit("closeButton");

		public static readonly StringName conditionEdit = StringName.op_Implicit("conditionEdit");

		public static readonly StringName conditionStatusLabel = StringName.op_Implicit("conditionStatusLabel");

		public static readonly StringName upToDateConditionEdit = StringName.op_Implicit("upToDateConditionEdit");

		public static readonly StringName upToDateConditionStatusLabel = StringName.op_Implicit("upToDateConditionStatusLabel");

		public static readonly StringName offsetXEdit = StringName.op_Implicit("offsetXEdit");

		public static readonly StringName offsetYEdit = StringName.op_Implicit("offsetYEdit");

		public static readonly StringName secondaryInitialStatesEdit = StringName.op_Implicit("secondaryInitialStatesEdit");

		public static readonly StringName stateMachineEdit = StringName.op_Implicit("stateMachineEdit");

		public static readonly StringName moveReplacementsEdit = StringName.op_Implicit("moveReplacementsEdit");

		public static readonly StringName graphPatchEdit = StringName.op_Implicit("graphPatchEdit");

		public static readonly StringName intentStringsStatusLabel = StringName.op_Implicit("intentStringsStatusLabel");

		public static readonly StringName intentStringsEdit = StringName.op_Implicit("intentStringsEdit");

		public static readonly StringName readOnlySummaryEdit = StringName.op_Implicit("readOnlySummaryEdit");

		public static readonly StringName previewStatusLabel = StringName.op_Implicit("previewStatusLabel");

		public static readonly StringName previewGraph = StringName.op_Implicit("previewGraph");

		public static readonly StringName unsavedChangesDialog = StringName.op_Implicit("unsavedChangesDialog");

		public static readonly StringName messageDialog = StringName.op_Implicit("messageDialog");

		public static readonly StringName windowPanel = StringName.op_Implicit("windowPanel");

		public static readonly StringName editorTabs = StringName.op_Implicit("editorTabs");

		public static readonly StringName monsterDisplayName = StringName.op_Implicit("monsterDisplayName");

		public static readonly StringName monsterModelFullName = StringName.op_Implicit("monsterModelFullName");

		public static readonly StringName loadedIntentStringsLanguage = StringName.op_Implicit("loadedIntentStringsLanguage");

		public static readonly StringName selectedVariantIndex = StringName.op_Implicit("selectedVariantIndex");

		public static readonly StringName isReady = StringName.op_Implicit("isReady");

		public static readonly StringName isRefreshingUi = StringName.op_Implicit("isRefreshingUi");

		public static readonly StringName hasUnsavedChanges = StringName.op_Implicit("hasUnsavedChanges");

		public static readonly StringName pendingAction = StringName.op_Implicit("pendingAction");

		public static readonly StringName lastActiveTextInput = StringName.op_Implicit("lastActiveTextInput");
	}

	public class SignalName : SignalName
	{
	}

	[CompilerGenerated]
	private sealed class _003CBuildGraphPatchCompletionItems_003Ed__104 : IEnumerable<CompletionItem>, IEnumerable, IEnumerator<CompletionItem>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private CompletionItem _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private bool isInsideString;

		public bool _003C_003E3__isInsideString;

		public NIntentGraphEditor _003C_003E4__this;

		private string[] _003C_003E7__wrap1;

		private int _003C_003E7__wrap2;

		private List<string>.Enumerator _003C_003E7__wrap3;

		CompletionItem IEnumerator<CompletionItem>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CBuildGraphPatchCompletionItems_003Ed__104(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			int num = _003C_003E1__state;
			if (num == -3 || num == 12)
			{
				try
				{
				}
				finally
				{
					_003C_003Em__Finally1();
				}
			}
			_003C_003E7__wrap1 = null;
			_003C_003E7__wrap3 = default(List<string>.Enumerator);
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			try
			{
				int num = _003C_003E1__state;
				NIntentGraphEditor nIntentGraphEditor = _003C_003E4__this;
				switch (num)
				{
				default:
					return false;
				case 0:
					_003C_003E1__state = -1;
					_003C_003E7__wrap1 = new string[21]
					{
						"width", "height", "moves", "icons", "iconGroups", "labels", "arrows", "x", "y", "id",
						"intentType", "value", "times", "valueText", "timesText", "text", "align", "path", "fontSize", "expand",
						"relativeTo"
					};
					_003C_003E7__wrap2 = 0;
					goto IL_0165;
				case 1:
					_003C_003E1__state = -1;
					_003C_003E7__wrap2++;
					goto IL_0165;
				case 2:
					_003C_003E1__state = -1;
					_003C_003E2__current = SnippetCompletion("graphPatch label", "{\n  \"x\": 0.0,\n  \"y\": 0.0,\n  \"text\": \"\",\n  \"align\": \"left\"\n}");
					_003C_003E1__state = 3;
					return true;
				case 3:
					_003C_003E1__state = -1;
					_003C_003E2__current = SnippetCompletion("graphPatch move", "{\n  \"x\": 0.0,\n  \"y\": 0.0,\n  \"id\": \"\"\n}");
					_003C_003E1__state = 4;
					return true;
				case 4:
					_003C_003E1__state = -1;
					_003C_003E2__current = SnippetCompletion("graphPatch icon", "{\n  \"x\": 0.0,\n  \"y\": 0.0,\n  \"intentType\": \"Attack\",\n  \"value\": 0,\n  \"times\": 1\n}");
					_003C_003E1__state = 5;
					return true;
				case 5:
					_003C_003E1__state = -1;
					_003C_003E2__current = SnippetCompletion("graphPatch iconGroup", "{\n  \"x\": 0.0,\n  \"y\": 0.0,\n  \"width\": 1.0,\n  \"height\": 1.0\n}");
					_003C_003E1__state = 6;
					return true;
				case 6:
					_003C_003E1__state = -1;
					_003C_003E2__current = SnippetCompletion("graphPatch arrow", "{\n  \"path\": [0, 0.0, 0.0, 1.0]\n}");
					_003C_003E1__state = 7;
					return true;
				case 7:
					_003C_003E1__state = -1;
					_003C_003E2__current = StringValueCompletion("left", isInsideString, (CodeCompletionKind)5);
					_003C_003E1__state = 8;
					return true;
				case 8:
					_003C_003E1__state = -1;
					_003C_003E2__current = StringValueCompletion("center", isInsideString, (CodeCompletionKind)5);
					_003C_003E1__state = 9;
					return true;
				case 9:
					_003C_003E1__state = -1;
					_003C_003E2__current = StringValueCompletion("right", isInsideString, (CodeCompletionKind)5);
					_003C_003E1__state = 10;
					return true;
				case 10:
					_003C_003E1__state = -1;
					_003C_003E7__wrap1 = Enum.GetNames<IntentType>();
					_003C_003E7__wrap2 = 0;
					goto IL_035d;
				case 11:
					_003C_003E1__state = -1;
					_003C_003E7__wrap2++;
					goto IL_035d;
				case 12:
					{
						_003C_003E1__state = -3;
						break;
					}
					IL_035d:
					if (_003C_003E7__wrap2 < _003C_003E7__wrap1.Length)
					{
						string value = _003C_003E7__wrap1[_003C_003E7__wrap2];
						_003C_003E2__current = StringValueCompletion(value, isInsideString, (CodeCompletionKind)5);
						_003C_003E1__state = 11;
						return true;
					}
					_003C_003E7__wrap1 = null;
					_003C_003E7__wrap3 = nIntentGraphEditor.availableStateIds.GetEnumerator();
					_003C_003E1__state = -3;
					break;
					IL_0165:
					if (_003C_003E7__wrap2 < _003C_003E7__wrap1.Length)
					{
						string propertyName = _003C_003E7__wrap1[_003C_003E7__wrap2];
						_003C_003E2__current = PropertyCompletion(propertyName, isInsideString, (CodeCompletionKind)4);
						_003C_003E1__state = 1;
						return true;
					}
					_003C_003E7__wrap1 = null;
					_003C_003E2__current = SnippetCompletion("graphPatch", "{\n  \"labels\": [\n    {\n      \"x\": 0.0,\n      \"y\": 0.0,\n      \"text\": \"\",\n      \"align\": \"left\"\n    }\n  ]\n}");
					_003C_003E1__state = 2;
					return true;
				}
				if (_003C_003E7__wrap3.MoveNext())
				{
					string current = _003C_003E7__wrap3.Current;
					_003C_003E2__current = StringValueCompletion(current, isInsideString, (CodeCompletionKind)3);
					_003C_003E1__state = 12;
					return true;
				}
				_003C_003Em__Finally1();
				_003C_003E7__wrap3 = default(List<string>.Enumerator);
				return false;
			}
			catch
			{
				//try-fault
				((IDisposable)this).Dispose();
				throw;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			((IDisposable)_003C_003E7__wrap3).Dispose();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<CompletionItem> IEnumerable<CompletionItem>.GetEnumerator()
		{
			_003CBuildGraphPatchCompletionItems_003Ed__104 _003CBuildGraphPatchCompletionItems_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CBuildGraphPatchCompletionItems_003Ed__ = this;
			}
			else
			{
				_003CBuildGraphPatchCompletionItems_003Ed__ = new _003CBuildGraphPatchCompletionItems_003Ed__104(0)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			_003CBuildGraphPatchCompletionItems_003Ed__.isInsideString = _003C_003E3__isInsideString;
			return _003CBuildGraphPatchCompletionItems_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<CompletionItem>)this).GetEnumerator();
		}
	}

	[CompilerGenerated]
	private sealed class _003CBuildMoveReplacementCompletionItems_003Ed__103 : IEnumerable<CompletionItem>, IEnumerable, IEnumerator<CompletionItem>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private CompletionItem _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private bool isInsideString;

		public bool _003C_003E3__isInsideString;

		public NIntentGraphEditor _003C_003E4__this;

		private string[] _003C_003E7__wrap1;

		private int _003C_003E7__wrap2;

		private List<string>.Enumerator _003C_003E7__wrap3;

		CompletionItem IEnumerator<CompletionItem>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CBuildMoveReplacementCompletionItems_003Ed__103(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			int num = _003C_003E1__state;
			if (num == -3 || num == 3)
			{
				try
				{
				}
				finally
				{
					_003C_003Em__Finally1();
				}
			}
			_003C_003E7__wrap1 = null;
			_003C_003E7__wrap3 = default(List<string>.Enumerator);
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			try
			{
				int num = _003C_003E1__state;
				NIntentGraphEditor nIntentGraphEditor = _003C_003E4__this;
				switch (num)
				{
				default:
					return false;
				case 0:
					_003C_003E1__state = -1;
					_003C_003E7__wrap1 = new string[10] { "intentOverrides", "arrowOverride", "path", "valueText", "timesText", "details", "type", "value", "id", "currentMoveCondition" };
					_003C_003E7__wrap2 = 0;
					goto IL_00e6;
				case 1:
					_003C_003E1__state = -1;
					_003C_003E7__wrap2++;
					goto IL_00e6;
				case 2:
					_003C_003E1__state = -1;
					_003C_003E7__wrap2++;
					goto IL_0171;
				case 3:
					_003C_003E1__state = -3;
					goto IL_01db;
				case 4:
					_003C_003E1__state = -1;
					_003C_003E2__current = PlainTextCompletion("null", "null");
					_003C_003E1__state = 5;
					return true;
				case 5:
					{
						_003C_003E1__state = -1;
						return false;
					}
					IL_00e6:
					if (_003C_003E7__wrap2 < _003C_003E7__wrap1.Length)
					{
						string propertyName = _003C_003E7__wrap1[_003C_003E7__wrap2];
						_003C_003E2__current = PropertyCompletion(propertyName, isInsideString, (CodeCompletionKind)4);
						_003C_003E1__state = 1;
						return true;
					}
					_003C_003E7__wrap1 = null;
					_003C_003E7__wrap1 = new string[3] { "none", "card", "power" };
					_003C_003E7__wrap2 = 0;
					goto IL_0171;
					IL_01db:
					if (_003C_003E7__wrap3.MoveNext())
					{
						string current = _003C_003E7__wrap3.Current;
						_003C_003E2__current = PropertyCompletion(current, isInsideString, (CodeCompletionKind)3);
						_003C_003E1__state = 3;
						return true;
					}
					_003C_003Em__Finally1();
					_003C_003E7__wrap3 = default(List<string>.Enumerator);
					_003C_003E2__current = SnippetCompletion("move replacement entry", "[\n  {\n    \"valueText\": \"\",\n    \"timesText\": \"\"\n  }\n]");
					_003C_003E1__state = 4;
					return true;
					IL_0171:
					if (_003C_003E7__wrap2 < _003C_003E7__wrap1.Length)
					{
						string value = _003C_003E7__wrap1[_003C_003E7__wrap2];
						_003C_003E2__current = StringValueCompletion(value, isInsideString, (CodeCompletionKind)5);
						_003C_003E1__state = 2;
						return true;
					}
					_003C_003E7__wrap1 = null;
					_003C_003E7__wrap3 = nIntentGraphEditor.availableStateIds.GetEnumerator();
					_003C_003E1__state = -3;
					goto IL_01db;
				}
			}
			catch
			{
				//try-fault
				((IDisposable)this).Dispose();
				throw;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			((IDisposable)_003C_003E7__wrap3).Dispose();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<CompletionItem> IEnumerable<CompletionItem>.GetEnumerator()
		{
			_003CBuildMoveReplacementCompletionItems_003Ed__103 _003CBuildMoveReplacementCompletionItems_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CBuildMoveReplacementCompletionItems_003Ed__ = this;
			}
			else
			{
				_003CBuildMoveReplacementCompletionItems_003Ed__ = new _003CBuildMoveReplacementCompletionItems_003Ed__103(0)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			_003CBuildMoveReplacementCompletionItems_003Ed__.isInsideString = _003C_003E3__isInsideString;
			return _003CBuildMoveReplacementCompletionItems_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<CompletionItem>)this).GetEnumerator();
		}
	}

	[CompilerGenerated]
	private sealed class _003CBuildSecondaryInitialStatesCompletionItems_003Ed__101 : IEnumerable<CompletionItem>, IEnumerable, IEnumerator<CompletionItem>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private CompletionItem _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private bool isInsideString;

		public bool _003C_003E3__isInsideString;

		public NIntentGraphEditor _003C_003E4__this;

		private string[] _003C_003E7__wrap1;

		private int _003C_003E7__wrap2;

		private List<string>.Enumerator _003C_003E7__wrap3;

		CompletionItem IEnumerator<CompletionItem>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CBuildSecondaryInitialStatesCompletionItems_003Ed__101(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			int num = _003C_003E1__state;
			if (num == -3 || num == 3)
			{
				try
				{
				}
				finally
				{
					_003C_003Em__Finally1();
				}
			}
			_003C_003E7__wrap1 = null;
			_003C_003E7__wrap3 = default(List<string>.Enumerator);
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			try
			{
				int num = _003C_003E1__state;
				NIntentGraphEditor nIntentGraphEditor = _003C_003E4__this;
				switch (num)
				{
				default:
					return false;
				case 0:
					_003C_003E1__state = -1;
					_003C_003E7__wrap1 = new string[4] { "id", "offset", "x", "y" };
					_003C_003E7__wrap2 = 0;
					goto IL_00ac;
				case 1:
					_003C_003E1__state = -1;
					_003C_003E7__wrap2++;
					goto IL_00ac;
				case 2:
					_003C_003E1__state = -1;
					_003C_003E7__wrap3 = nIntentGraphEditor.availableStateIds.GetEnumerator();
					_003C_003E1__state = -3;
					break;
				case 3:
					{
						_003C_003E1__state = -3;
						break;
					}
					IL_00ac:
					if (_003C_003E7__wrap2 < _003C_003E7__wrap1.Length)
					{
						string propertyName = _003C_003E7__wrap1[_003C_003E7__wrap2];
						_003C_003E2__current = PropertyCompletion(propertyName, isInsideString, (CodeCompletionKind)4);
						_003C_003E1__state = 1;
						return true;
					}
					_003C_003E7__wrap1 = null;
					_003C_003E2__current = SnippetCompletion("secondary initial states", "[\n  \"\"\n]");
					_003C_003E1__state = 2;
					return true;
				}
				if (_003C_003E7__wrap3.MoveNext())
				{
					string current = _003C_003E7__wrap3.Current;
					_003C_003E2__current = StringValueCompletion(current, isInsideString, (CodeCompletionKind)3);
					_003C_003E1__state = 3;
					return true;
				}
				_003C_003Em__Finally1();
				_003C_003E7__wrap3 = default(List<string>.Enumerator);
				return false;
			}
			catch
			{
				//try-fault
				((IDisposable)this).Dispose();
				throw;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			((IDisposable)_003C_003E7__wrap3).Dispose();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<CompletionItem> IEnumerable<CompletionItem>.GetEnumerator()
		{
			_003CBuildSecondaryInitialStatesCompletionItems_003Ed__101 _003CBuildSecondaryInitialStatesCompletionItems_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CBuildSecondaryInitialStatesCompletionItems_003Ed__ = this;
			}
			else
			{
				_003CBuildSecondaryInitialStatesCompletionItems_003Ed__ = new _003CBuildSecondaryInitialStatesCompletionItems_003Ed__101(0)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			_003CBuildSecondaryInitialStatesCompletionItems_003Ed__.isInsideString = _003C_003E3__isInsideString;
			return _003CBuildSecondaryInitialStatesCompletionItems_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<CompletionItem>)this).GetEnumerator();
		}
	}

	[CompilerGenerated]
	private sealed class _003CBuildStateMachineCompletionItems_003Ed__102 : IEnumerable<CompletionItem>, IEnumerable, IEnumerator<CompletionItem>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private CompletionItem _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private bool isInsideString;

		public bool _003C_003E3__isInsideString;

		public NIntentGraphEditor _003C_003E4__this;

		private string[] _003C_003E7__wrap1;

		private int _003C_003E7__wrap2;

		private List<string>.Enumerator _003C_003E7__wrap3;

		CompletionItem IEnumerator<CompletionItem>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CBuildStateMachineCompletionItems_003Ed__102(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			int num = _003C_003E1__state;
			if (num == -3 || num == 6)
			{
				try
				{
				}
				finally
				{
					_003C_003Em__Finally1();
				}
			}
			_003C_003E7__wrap1 = null;
			_003C_003E7__wrap3 = default(List<string>.Enumerator);
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			try
			{
				int num = _003C_003E1__state;
				NIntentGraphEditor nIntentGraphEditor = _003C_003E4__this;
				switch (num)
				{
				default:
					return false;
				case 0:
					_003C_003E1__state = -1;
					_003C_003E7__wrap1 = new string[15]
					{
						"name", "moveName", "isInitialState", "initialStatePriority", "children", "followUpState", "label", "node", "horizontalLayout", "placeholderIntentCount",
						"notSimpleLoopStart", "alternativeMoveNames", "offset", "x", "y"
					};
					_003C_003E7__wrap2 = 0;
					goto IL_0117;
				case 1:
					_003C_003E1__state = -1;
					_003C_003E7__wrap2++;
					goto IL_0117;
				case 2:
					_003C_003E1__state = -1;
					_003C_003E2__current = SnippetCompletion("state machine branch node", "{\n  \"name\": \"\",\n  \"children\": [\n    {\n      \"label\": \"\",\n      \"node\": {\n        \"name\": \"\"\n      }\n    }\n  ]\n}");
					_003C_003E1__state = 3;
					return true;
				case 3:
					_003C_003E1__state = -1;
					_003C_003E2__current = BooleanCompletion(value: true);
					_003C_003E1__state = 4;
					return true;
				case 4:
					_003C_003E1__state = -1;
					_003C_003E2__current = BooleanCompletion(value: false);
					_003C_003E1__state = 5;
					return true;
				case 5:
					_003C_003E1__state = -1;
					_003C_003E7__wrap3 = nIntentGraphEditor.availableStateIds.GetEnumerator();
					_003C_003E1__state = -3;
					break;
				case 6:
					{
						_003C_003E1__state = -3;
						break;
					}
					IL_0117:
					if (_003C_003E7__wrap2 < _003C_003E7__wrap1.Length)
					{
						string propertyName = _003C_003E7__wrap1[_003C_003E7__wrap2];
						_003C_003E2__current = PropertyCompletion(propertyName, isInsideString, (CodeCompletionKind)4);
						_003C_003E1__state = 1;
						return true;
					}
					_003C_003E7__wrap1 = null;
					_003C_003E2__current = SnippetCompletion("state machine node", "{\n  \"name\": \"\",\n  \"moveName\": \"\",\n  \"isInitialState\": false,\n  \"initialStatePriority\": 0,\n  \"followUpState\": \"\"\n}");
					_003C_003E1__state = 2;
					return true;
				}
				if (_003C_003E7__wrap3.MoveNext())
				{
					string current = _003C_003E7__wrap3.Current;
					_003C_003E2__current = StringValueCompletion(current, isInsideString, (CodeCompletionKind)3);
					_003C_003E1__state = 6;
					return true;
				}
				_003C_003Em__Finally1();
				_003C_003E7__wrap3 = default(List<string>.Enumerator);
				return false;
			}
			catch
			{
				//try-fault
				((IDisposable)this).Dispose();
				throw;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			((IDisposable)_003C_003E7__wrap3).Dispose();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<CompletionItem> IEnumerable<CompletionItem>.GetEnumerator()
		{
			_003CBuildStateMachineCompletionItems_003Ed__102 _003CBuildStateMachineCompletionItems_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CBuildStateMachineCompletionItems_003Ed__ = this;
			}
			else
			{
				_003CBuildStateMachineCompletionItems_003Ed__ = new _003CBuildStateMachineCompletionItems_003Ed__102(0)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			_003CBuildStateMachineCompletionItems_003Ed__.isInsideString = _003C_003E3__isInsideString;
			return _003CBuildStateMachineCompletionItems_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<CompletionItem>)this).GetEnumerator();
		}
	}

	private Label? monsterNameLabel;

	private Label? monsterModelLabel;

	private Label? headerStatusLabel;

	private ItemList? variantList;

	private ItemList? stateIdList;

	private Button? addVariantButton;

	private Button? duplicateVariantButton;

	private Button? deleteVariantButton;

	private Button? moveVariantUpButton;

	private Button? moveVariantDownButton;

	private Button? saveButton;

	private Button? reloadButton;

	private Button? closeButton;

	private LineEdit? conditionEdit;

	private Label? conditionStatusLabel;

	private LineEdit? upToDateConditionEdit;

	private Label? upToDateConditionStatusLabel;

	private LineEdit? offsetXEdit;

	private LineEdit? offsetYEdit;

	private CodeEdit? secondaryInitialStatesEdit;

	private CodeEdit? stateMachineEdit;

	private CodeEdit? moveReplacementsEdit;

	private CodeEdit? graphPatchEdit;

	private Label? intentStringsStatusLabel;

	private CodeEdit? intentStringsEdit;

	private CodeEdit? readOnlySummaryEdit;

	private Label? previewStatusLabel;

	private NIntentGraph? previewGraph;

	private ConfirmationDialog? unsavedChangesDialog;

	private AcceptDialog? messageDialog;

	private PanelContainer? windowPanel;

	private TabContainer? editorTabs;

	private MonsterModel? monster;

	private string monsterDisplayName = string.Empty;

	private string monsterModelFullName = string.Empty;

	private string loadedIntentStringsLanguage = "eng";

	private IntentDefinitionList draftDefinitions = new IntentDefinitionList();

	private Dictionary<string, string> draftIntentStrings = new Dictionary<string, string>(StringComparer.Ordinal);

	private List<string> availableStateIds = new List<string>();

	private int selectedVariantIndex = -1;

	private bool isReady;

	private bool isRefreshingUi;

	private bool hasUnsavedChanges;

	private PendingAction pendingAction;

	private Control? lastActiveTextInput;

	public bool HasUnsavedChanges => hasUnsavedChanges;

	private static string LocalizeText(string key, string fallback)
	{
		return IntentGraphMod.IntentGraphStrings.GetValueOrDefault(key, fallback);
	}

	private static string LocalizeText(string key, string fallback, params object[] args)
	{
		return string.Format(LocalizeText(key, fallback), args);
	}

	public void Initialize(MonsterModel monster, string monsterDisplayName)
	{
		this.monster = monster;
		this.monsterDisplayName = monsterDisplayName;
		monsterModelFullName = ((object)monster).GetType().FullName ?? ((object)((AbstractModel)monster).Id).ToString();
		if (isReady)
		{
			LoadDefinitionsFromDiskOrRuntime();
		}
	}

	public override void _Ready()
	{
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Expected O, but got Unknown
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Expected O, but got Unknown
		//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c3: Expected O, but got Unknown
		//IL_03d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03da: Expected O, but got Unknown
		//IL_03e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f1: Expected O, but got Unknown
		//IL_03fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0408: Expected O, but got Unknown
		//IL_061f: Unknown result type (might be due to invalid IL or missing references)
		monsterNameLabel = ((Node)this).GetNode<Label>(NodePath.op_Implicit("%MonsterName"));
		monsterModelLabel = ((Node)this).GetNode<Label>(NodePath.op_Implicit("%MonsterModelName"));
		headerStatusLabel = ((Node)this).GetNode<Label>(NodePath.op_Implicit("%HeaderStatus"));
		variantList = ((Node)this).GetNode<ItemList>(NodePath.op_Implicit("%VariantList"));
		stateIdList = ((Node)this).GetNode<ItemList>(NodePath.op_Implicit("%StateIdList"));
		addVariantButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%AddVariantButton"));
		duplicateVariantButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%DuplicateVariantButton"));
		deleteVariantButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%DeleteVariantButton"));
		moveVariantUpButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%MoveVariantUpButton"));
		moveVariantDownButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%MoveVariantDownButton"));
		saveButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%SaveButton"));
		reloadButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%ReloadButton"));
		closeButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%CloseButton"));
		conditionEdit = ((Node)this).GetNode<LineEdit>(NodePath.op_Implicit("%ConditionEdit"));
		conditionStatusLabel = ((Node)this).GetNode<Label>(NodePath.op_Implicit("%ConditionStatus"));
		upToDateConditionEdit = ((Node)this).GetNode<LineEdit>(NodePath.op_Implicit("%UpToDateConditionEdit"));
		upToDateConditionStatusLabel = ((Node)this).GetNode<Label>(NodePath.op_Implicit("%UpToDateConditionStatus"));
		offsetXEdit = ((Node)this).GetNode<LineEdit>(NodePath.op_Implicit("%OffsetXEdit"));
		offsetYEdit = ((Node)this).GetNode<LineEdit>(NodePath.op_Implicit("%OffsetYEdit"));
		secondaryInitialStatesEdit = ((Node)this).GetNode<CodeEdit>(NodePath.op_Implicit("%SecondaryInitialStatesEdit"));
		stateMachineEdit = ((Node)this).GetNode<CodeEdit>(NodePath.op_Implicit("%StateMachineEdit"));
		moveReplacementsEdit = ((Node)this).GetNode<CodeEdit>(NodePath.op_Implicit("%MoveReplacementsEdit"));
		graphPatchEdit = ((Node)this).GetNode<CodeEdit>(NodePath.op_Implicit("%GraphPatchEdit"));
		intentStringsStatusLabel = ((Node)this).GetNode<Label>(NodePath.op_Implicit("%IntentStringsStatus"));
		intentStringsEdit = ((Node)this).GetNode<CodeEdit>(NodePath.op_Implicit("%IntentStringsEdit"));
		readOnlySummaryEdit = ((Node)this).GetNode<CodeEdit>(NodePath.op_Implicit("%ReadOnlySummaryEdit"));
		previewStatusLabel = ((Node)this).GetNode<Label>(NodePath.op_Implicit("%PreviewStatus"));
		previewGraph = ((Node)this).GetNode<NIntentGraph>(NodePath.op_Implicit("%PreviewGraph"));
		unsavedChangesDialog = ((Node)this).GetNode<ConfirmationDialog>(NodePath.op_Implicit("%UnsavedChangesDialog"));
		messageDialog = ((Node)this).GetNode<AcceptDialog>(NodePath.op_Implicit("%MessageDialog"));
		windowPanel = ((Node)this).GetNode<PanelContainer>(NodePath.op_Implicit("WindowMargin/Window"));
		editorTabs = ((Node)this).GetNode<TabContainer>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/EditorScroll/EditorVBox/EditorTabs"));
		ApplyLocalization();
		variantList.ItemSelected += new ItemSelectedEventHandler(OnVariantSelected);
		stateIdList.ItemClicked += new ItemClickedEventHandler(OnStateIdClicked);
		((BaseButton)addVariantButton).Pressed += OnAddVariantPressed;
		((BaseButton)duplicateVariantButton).Pressed += OnDuplicateVariantPressed;
		((BaseButton)deleteVariantButton).Pressed += OnDeleteVariantPressed;
		((BaseButton)moveVariantUpButton).Pressed += OnMoveVariantUpPressed;
		((BaseButton)moveVariantDownButton).Pressed += OnMoveVariantDownPressed;
		((BaseButton)saveButton).Pressed += OnSavePressed;
		((BaseButton)reloadButton).Pressed += OnReloadPressed;
		((BaseButton)closeButton).Pressed += OnClosePressed;
		conditionEdit.TextChanged += (TextChangedEventHandler)delegate
		{
			OnEditorFieldsChanged();
		};
		upToDateConditionEdit.TextChanged += (TextChangedEventHandler)delegate
		{
			OnEditorFieldsChanged();
		};
		offsetXEdit.TextChanged += (TextChangedEventHandler)delegate
		{
			OnEditorFieldsChanged();
		};
		offsetYEdit.TextChanged += (TextChangedEventHandler)delegate
		{
			OnEditorFieldsChanged();
		};
		((TextEdit)secondaryInitialStatesEdit).TextChanged += OnEditorFieldsChanged;
		((TextEdit)stateMachineEdit).TextChanged += OnEditorFieldsChanged;
		((TextEdit)moveReplacementsEdit).TextChanged += OnEditorFieldsChanged;
		((TextEdit)graphPatchEdit).TextChanged += OnEditorFieldsChanged;
		((TextEdit)intentStringsEdit).TextChanged += OnIntentStringsChanged;
		((TextEdit)secondaryInitialStatesEdit).TextChanged += delegate
		{
			OnCodeEditorTextChanged(secondaryInitialStatesEdit);
		};
		((TextEdit)stateMachineEdit).TextChanged += delegate
		{
			OnCodeEditorTextChanged(stateMachineEdit);
		};
		((TextEdit)moveReplacementsEdit).TextChanged += delegate
		{
			OnCodeEditorTextChanged(moveReplacementsEdit);
		};
		((TextEdit)graphPatchEdit).TextChanged += delegate
		{
			OnCodeEditorTextChanged(graphPatchEdit);
		};
		secondaryInitialStatesEdit.CodeCompletionRequested += delegate
		{
			OnCodeCompletionRequested(secondaryInitialStatesEdit, JsonEditorKind.SecondaryInitialStates);
		};
		stateMachineEdit.CodeCompletionRequested += delegate
		{
			OnCodeCompletionRequested(stateMachineEdit, JsonEditorKind.StateMachine);
		};
		moveReplacementsEdit.CodeCompletionRequested += delegate
		{
			OnCodeCompletionRequested(moveReplacementsEdit, JsonEditorKind.MoveReplacements);
		};
		graphPatchEdit.CodeCompletionRequested += delegate
		{
			OnCodeCompletionRequested(graphPatchEdit, JsonEditorKind.GraphPatch);
		};
		((AcceptDialog)unsavedChangesDialog).Confirmed += OnUnsavedChangesConfirmed;
		RegisterEditableInput((Control?)(object)conditionEdit);
		RegisterEditableInput((Control?)(object)upToDateConditionEdit);
		RegisterEditableInput((Control?)(object)secondaryInitialStatesEdit);
		RegisterEditableInput((Control?)(object)stateMachineEdit);
		RegisterEditableInput((Control?)(object)moveReplacementsEdit);
		RegisterEditableInput((Control?)(object)graphPatchEdit);
		RegisterEditableInput((Control?)(object)intentStringsEdit);
		ConfigureCodeEditor(secondaryInitialStatesEdit);
		ConfigureCodeEditor(stateMachineEdit);
		ConfigureCodeEditor(moveReplacementsEdit);
		ConfigureCodeEditor(graphPatchEdit);
		ConfigureCodeEditor(intentStringsEdit, enableCompletion: false);
		ConfigureCodeEditor(readOnlySummaryEdit, enableCompletion: false);
		previewGraph.AnimatedIcons = IntentGraphMod.Config.UseAnimatedIntentIcon;
		((TextEdit)readOnlySummaryEdit).Editable = false;
		isReady = true;
		((GodotObject)this).CallDeferred(MethodName.ApplyViewportLayout, Array.Empty<Variant>());
		if (monster != null)
		{
			LoadDefinitionsFromDiskOrRuntime();
			return;
		}
		UpdateHeaderStatus(LocalizeText("ui.editor.status.no_monster", "No monster selected."));
		SetEditorsEnabled(enabled: false);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Invalid comparison between Unknown and I8
		InputEventKey val = (InputEventKey)(object)((@event is InputEventKey) ? @event : null);
		if (val != null && val.Pressed && !val.Echo && (long)val.Keycode == 4194305)
		{
			RequestClose();
			((Node)this).GetViewport().SetInputAsHandled();
		}
	}

	private void ApplyViewportLayout()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		Rect2 viewportRect = ((CanvasItem)this).GetViewportRect();
		Vector2 size = ((Rect2)(ref viewportRect)).Size;
		((Control)this).Position = Vector2.Zero;
		((Control)this).Size = size;
		if (windowPanel != null)
		{
			((Control)windowPanel).CustomMinimumSize = size;
			((Control)windowPanel).Size = size;
		}
	}

	private void LoadDefinitionsFromDiskOrRuntime()
	{
		MonsterModel? obj = monster;
		object obj2;
		if (obj == null)
		{
			obj2 = null;
		}
		else
		{
			MonsterMoveStateMachine moveStateMachine = obj.MoveStateMachine;
			obj2 = ((moveStateMachine != null) ? (from state in moveStateMachine.States.Values
				select state.Id into id
				where !string.IsNullOrWhiteSpace(id)
				select id).Distinct<string>(StringComparer.Ordinal).OrderBy<string, string>((string id) => id, StringComparer.Ordinal).ToList() : null);
		}
		if (obj2 == null)
		{
			obj2 = new List<string>();
		}
		availableStateIds = (List<string>)obj2;
		draftDefinitions = ((monster == null) ? new IntentDefinitionList() : IntentDefinitionEditorService.LoadEditableDefinitions(monsterModelFullName));
		loadedIntentStringsLanguage = LocManager.Instance.Language;
		draftIntentStrings = IntentGraphMod.LoadIntentStringsFromFile(IntentGraphMod.GetDevIntentStringFilePath(loadedIntentStringsLanguage));
		ApplyLocalization();
		selectedVariantIndex = ((draftDefinitions.Count > 0) ? Math.Clamp(selectedVariantIndex, 0, draftDefinitions.Count - 1) : (-1));
		if (draftDefinitions.Count > 0 && selectedVariantIndex < 0)
		{
			selectedVariantIndex = 0;
		}
		hasUnsavedChanges = false;
		pendingAction = PendingAction.None;
		RefreshUiFromState();
		UpdateHeaderStatus(LocalizeText("ui.editor.status.editing_monster", "Editing {0}", monsterModelFullName));
	}

	private void ApplyLocalization()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		Label? obj = monsterNameLabel;
		if (obj != null)
		{
			((GodotObject)obj).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.header.monster_name", "Monster Name")));
		}
		Label? obj2 = monsterModelLabel;
		if (obj2 != null)
		{
			((GodotObject)obj2).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.header.monster_model_name", "Monster model full name")));
		}
		Label? obj3 = headerStatusLabel;
		if (!string.IsNullOrWhiteSpace((obj3 != null) ? obj3.Text : null))
		{
			Label? obj4 = headerStatusLabel;
			if (!(((obj4 != null) ? obj4.Text : null) == "Ready"))
			{
				goto IL_00aa;
			}
		}
		UpdateHeaderStatus(LocalizeText("ui.editor.status.ready", "Ready"));
		goto IL_00aa;
		IL_00aa:
		Button? obj5 = saveButton;
		if (obj5 != null)
		{
			((GodotObject)obj5).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.button.save", "Save")));
		}
		Button? obj6 = reloadButton;
		if (obj6 != null)
		{
			((GodotObject)obj6).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.button.reload", "Reload")));
		}
		Button? obj7 = closeButton;
		if (obj7 != null)
		{
			((GodotObject)obj7).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.button.close", "Close")));
		}
		Button? obj8 = addVariantButton;
		if (obj8 != null)
		{
			((GodotObject)obj8).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.button.add", "Add")));
		}
		Button? obj9 = duplicateVariantButton;
		if (obj9 != null)
		{
			((GodotObject)obj9).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.button.duplicate", "Duplicate")));
		}
		Button? obj10 = deleteVariantButton;
		if (obj10 != null)
		{
			((GodotObject)obj10).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.button.delete", "Delete")));
		}
		Button? obj11 = moveVariantUpButton;
		if (obj11 != null)
		{
			((GodotObject)obj11).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.button.move_up", "Move Up")));
		}
		Button? obj12 = moveVariantDownButton;
		if (obj12 != null)
		{
			((GodotObject)obj12).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.button.move_down", "Move Down")));
		}
		((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/Sidebar/VariantsLabel")).Text = LocalizeText("ui.editor.sidebar.variants", "Variants");
		((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/Sidebar/VariantHint")).Text = LocalizeText("ui.editor.sidebar.variant_hint", "Later items win when multiple conditions match.");
		((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/Sidebar/StateIdsLabel")).Text = LocalizeText("ui.editor.sidebar.state_ids", "State IDs");
		((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/Sidebar/StateIdHint")).Text = LocalizeText("ui.editor.sidebar.state_ids_hint", "Click a state ID to insert it into the last focused editable field.");
		((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/EditorScroll/EditorVBox/EditorTabs/Condition/ConditionSection/ConditionLabel")).Text = LocalizeText("ui.editor.condition.label", "Selection condition");
		conditionEdit.PlaceholderText = LocalizeText("ui.editor.condition.placeholder", "true");
		((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/EditorScroll/EditorVBox/EditorTabs/Condition/ConditionSection/UpToDateConditionLabel")).Text = LocalizeText("ui.editor.up_to_date_condition.label", "Up-to-date condition");
		upToDateConditionEdit.PlaceholderText = LocalizeText("ui.editor.up_to_date_condition.placeholder", "Leave blank to skip outdated warnings.");
		((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/EditorScroll/EditorVBox/EditorTabs/Condition/ConditionSection/OffsetRow/OffsetXLabel")).Text = LocalizeText("ui.editor.offset_x.label", "offsetX");
		offsetXEdit.PlaceholderText = LocalizeText("ui.editor.offset_x.placeholder", "0");
		((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/EditorScroll/EditorVBox/EditorTabs/Condition/ConditionSection/OffsetRow/OffsetYLabel")).Text = LocalizeText("ui.editor.offset_y.label", "offsetY");
		offsetYEdit.PlaceholderText = LocalizeText("ui.editor.offset_y.placeholder", "0");
		((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/EditorScroll/EditorVBox/EditorTabs/secondaryInitialStates JSON/SecondaryInitialStatesSection/SecondaryInitialStatesHelp")).Text = LocalizeText("ui.editor.help.secondary_initial_states", "Edit secondaryInitialStates as a JSON array of state ids.");
		((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/EditorScroll/EditorVBox/EditorTabs/stateMachine JSON/StateMachineSection/StateMachineHelp")).Text = LocalizeText("ui.editor.help.state_machine", "Edit the StateMachineNode array for this variant.");
		((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/EditorScroll/EditorVBox/EditorTabs/moveReplacements JSON/MoveReplacementsSection/MoveReplacementHelp")).Text = LocalizeText("ui.editor.help.move_replacements", "Edit move replacement overrides keyed by move id.");
		((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/EditorScroll/EditorVBox/EditorTabs/graphPatch JSON/GraphPatchSection/GraphPatchHelp")).Text = LocalizeText("ui.editor.help.graph_patch", "Edit the full graphPatch object for this variant.");
		((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/PreviewVBox/PreviewTitle")).Text = LocalizeText("ui.editor.preview.title", "Live Preview");
		if (editorTabs != null)
		{
			SetTabTitle("Condition", LocalizeText("ui.editor.tab.condition", "Basic Value"));
			SetTabTitle("secondaryInitialStates JSON", LocalizeText("ui.editor.tab.secondary_initial_states", "secondaryInitialStates JSON"));
			SetTabTitle("stateMachine JSON", LocalizeText("ui.editor.tab.state_machine", "stateMachine JSON"));
			SetTabTitle("moveReplacements JSON", LocalizeText("ui.editor.tab.move_replacements", "moveReplacements JSON"));
			SetTabTitle("graphPatch JSON", LocalizeText("ui.editor.tab.graph_patch", "graphPatch JSON"));
			SetTabTitle("intentgraph strings JSON", LocalizeText("ui.editor.tab.intent_strings", "intentgraph strings JSON"));
			SetTabTitle("Read-only fields", LocalizeText("ui.editor.tab.read_only", "Read-only fields"));
		}
		((Window)unsavedChangesDialog).Title = LocalizeText("ui.editor.dialog.unsaved.title", "Discard Changes?");
		((AcceptDialog)unsavedChangesDialog).DialogText = LocalizeText("ui.editor.dialog.unsaved.text", "You have unsaved changes. Continue and discard them?");
		((Window)messageDialog).Title = LocalizeText("ui.editor.dialog.message.title", "Intent Graph Editor");
		if (conditionStatusLabel != null && conditionStatusLabel.Text == "Condition syntax is validated against the current monster.")
		{
			conditionStatusLabel.Text = LocalizeText("ui.editor.condition.status.initial", "Condition syntax is validated against the current monster.");
		}
		if (upToDateConditionStatusLabel != null && upToDateConditionStatusLabel.Text == "Leave blank to skip outdated warnings. Syntax is validated against the current monster.")
		{
			upToDateConditionStatusLabel.Text = LocalizeText("ui.editor.up_to_date_condition.status.initial", "Leave blank to skip outdated warnings. Syntax is validated against the current monster.");
		}
		if (intentStringsStatusLabel != null && intentStringsStatusLabel.Text == "Editing dev intent strings for the current language.")
		{
			intentStringsStatusLabel.Text = LocalizeText("ui.editor.intent_strings.status.initial", "Editing dev intent strings for the current language.");
		}
		if (previewStatusLabel != null && previewStatusLabel.Text == "Preview updates as fields become valid.")
		{
			previewStatusLabel.Text = LocalizeText("ui.editor.preview.status.initial", "Preview updates as fields become valid.");
		}
	}

	private void SetTabTitle(string childName, string title)
	{
		if (editorTabs != null && ((Node)editorTabs).HasNode(NodePath.op_Implicit(childName)))
		{
			Control node = ((Node)editorTabs).GetNode<Control>(NodePath.op_Implicit(childName));
			editorTabs.SetTabTitle(((Node)node).GetIndex(false), title);
		}
	}

	private void RefreshUiFromState()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		if (!isReady)
		{
			return;
		}
		isRefreshingUi = true;
		try
		{
			Label? obj = monsterNameLabel;
			if (obj != null)
			{
				((GodotObject)obj).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(monsterDisplayName));
			}
			Label? obj2 = monsterModelLabel;
			if (obj2 != null)
			{
				((GodotObject)obj2).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(monsterModelFullName));
			}
			RefreshVariantList();
			RefreshStateIdList();
			LoadSelectedDefinitionIntoEditors();
			LoadIntentStringsIntoEditor();
			UpdateActionState();
		}
		finally
		{
			isRefreshingUi = false;
		}
		RefreshPreview();
	}

	private void RefreshVariantList()
	{
		if (variantList != null)
		{
			variantList.Clear();
			for (int i = 0; i < draftDefinitions.Count; i++)
			{
				variantList.AddItem(BuildVariantLabel(i, draftDefinitions[i]), (Texture2D)null, true);
			}
			if (selectedVariantIndex >= 0 && selectedVariantIndex < draftDefinitions.Count)
			{
				variantList.Select(selectedVariantIndex, true);
			}
		}
	}

	private void RefreshStateIdList()
	{
		if (stateIdList == null)
		{
			return;
		}
		stateIdList.Clear();
		foreach (string availableStateId in availableStateIds)
		{
			stateIdList.AddItem(availableStateId, (Texture2D)null, true);
		}
	}

	private static string BuildVariantLabel(int index, IntentDefinition definition)
	{
		string text = (string.IsNullOrWhiteSpace(definition.Condition) ? "true" : definition.Condition.Trim());
		if (text.Length > 42)
		{
			text = text.Substring(0, 39) + "...";
		}
		return $"#{index + 1} {text}";
	}

	private void LoadSelectedDefinitionIntoEditors()
	{
		IntentDefinition intentDefinition = ((selectedVariantIndex >= 0 && selectedVariantIndex < draftDefinitions.Count) ? draftDefinitions[selectedVariantIndex] : null);
		SetEditorsEnabled(intentDefinition != null);
		if (conditionEdit != null && upToDateConditionEdit != null && offsetXEdit != null && offsetYEdit != null && secondaryInitialStatesEdit != null && stateMachineEdit != null && moveReplacementsEdit != null && graphPatchEdit != null && readOnlySummaryEdit != null)
		{
			if (intentDefinition == null)
			{
				conditionEdit.Text = string.Empty;
				upToDateConditionEdit.Text = string.Empty;
				offsetXEdit.Text = string.Empty;
				offsetYEdit.Text = string.Empty;
				((TextEdit)secondaryInitialStatesEdit).Text = string.Empty;
				((TextEdit)stateMachineEdit).Text = string.Empty;
				((TextEdit)moveReplacementsEdit).Text = string.Empty;
				((TextEdit)graphPatchEdit).Text = string.Empty;
				((TextEdit)readOnlySummaryEdit).Text = LocalizeText("ui.editor.message.no_variant_selected", "No variant selected. Add one to begin editing.");
				UpdateConditionStatus(null);
				UpdateUpToDateConditionStatus(null);
			}
			else
			{
				conditionEdit.Text = intentDefinition.Condition ?? "true";
				upToDateConditionEdit.Text = intentDefinition.UpToDateCondition ?? string.Empty;
				offsetXEdit.Text = FormatPositionComponent(intentDefinition.Offset.X);
				offsetYEdit.Text = FormatPositionComponent(intentDefinition.Offset.Y);
				((TextEdit)secondaryInitialStatesEdit).Text = ((intentDefinition.SecondaryInitialStates == null || intentDefinition.SecondaryInitialStates.Length == 0) ? string.Empty : IntentDefinitionEditorService.SerializeJson(intentDefinition.SecondaryInitialStates));
				((TextEdit)stateMachineEdit).Text = ((intentDefinition.StateMachine == null) ? string.Empty : IntentDefinitionEditorService.SerializeJson(intentDefinition.StateMachine));
				((TextEdit)moveReplacementsEdit).Text = ((intentDefinition.MoveReplacements == null) ? string.Empty : IntentDefinitionEditorService.SerializeJson(intentDefinition.MoveReplacements));
				((TextEdit)graphPatchEdit).Text = ((intentDefinition.GraphPatch == null) ? string.Empty : IntentDefinitionEditorService.SerializeJson(intentDefinition.GraphPatch));
				((TextEdit)readOnlySummaryEdit).Text = IntentDefinitionEditorService.BuildReadOnlySummary(intentDefinition);
				UpdateConditionStatus(intentDefinition.Condition);
				UpdateUpToDateConditionStatus(intentDefinition.UpToDateCondition);
			}
		}
	}

	private void LoadIntentStringsIntoEditor()
	{
		if (intentStringsEdit != null)
		{
			((TextEdit)intentStringsEdit).Text = ((draftIntentStrings.Count == 0) ? string.Empty : IntentDefinitionEditorService.SerializeJson(draftIntentStrings));
			UpdateIntentStringsStatus(null);
		}
	}

	private void UpdateActionState()
	{
		bool flag = selectedVariantIndex >= 0 && selectedVariantIndex < draftDefinitions.Count;
		if (duplicateVariantButton != null)
		{
			((BaseButton)duplicateVariantButton).Disabled = !flag;
		}
		if (deleteVariantButton != null)
		{
			((BaseButton)deleteVariantButton).Disabled = !flag;
		}
		if (moveVariantUpButton != null)
		{
			((BaseButton)moveVariantUpButton).Disabled = !flag || selectedVariantIndex <= 0;
		}
		if (moveVariantDownButton != null)
		{
			((BaseButton)moveVariantDownButton).Disabled = !flag || selectedVariantIndex >= draftDefinitions.Count - 1;
		}
	}

	private void SetEditorsEnabled(bool enabled)
	{
		if (conditionEdit != null)
		{
			conditionEdit.Editable = enabled;
		}
		if (upToDateConditionEdit != null)
		{
			upToDateConditionEdit.Editable = enabled;
		}
		if (offsetXEdit != null)
		{
			offsetXEdit.Editable = enabled;
		}
		if (offsetYEdit != null)
		{
			offsetYEdit.Editable = enabled;
		}
		if (secondaryInitialStatesEdit != null)
		{
			((TextEdit)secondaryInitialStatesEdit).Editable = enabled;
		}
		if (stateMachineEdit != null)
		{
			((TextEdit)stateMachineEdit).Editable = enabled;
		}
		if (moveReplacementsEdit != null)
		{
			((TextEdit)moveReplacementsEdit).Editable = enabled;
		}
		if (graphPatchEdit != null)
		{
			((TextEdit)graphPatchEdit).Editable = enabled;
		}
	}

	private void OnEditorFieldsChanged()
	{
		if (!isRefreshingUi)
		{
			hasUnsavedChanges = true;
			UpdateHeaderStatus(LocalizeText("ui.editor.status.unsaved_changes", "Unsaved changes"));
			RefreshPreview();
		}
	}

	private void OnIntentStringsChanged()
	{
		OnEditorFieldsChanged();
		if (TryReadIntentStringsFromEditor(out Dictionary<string, string> _, out string error))
		{
			UpdateIntentStringsStatus(null);
		}
		else
		{
			UpdateIntentStringsStatus(error);
		}
	}

	private void ConfigureCodeEditor(CodeEdit? editor, bool enableCompletion = true)
	{
		if (editor != null)
		{
			editor.CodeCompletionEnabled = enableCompletion;
			editor.AutoBraceCompletionEnabled = true;
			editor.IndentAutomatic = true;
			editor.IndentUseSpaces = true;
			editor.IndentSize = 4;
			editor.GuttersDrawLineNumbers = true;
		}
	}

	private void OnCodeEditorTextChanged(CodeEdit? editor)
	{
		if (editor != null && !isRefreshingUi && ((Control)editor).HasFocus())
		{
			string line = ((TextEdit)editor).GetLine(((TextEdit)editor).GetCaretLine(0));
			int caretColumn = ((TextEdit)editor).GetCaretColumn(0);
			char c = ((caretColumn > 0 && caretColumn <= line.Length) ? line[caretColumn - 1] : '\0');
			string completionPrefix = GetCompletionPrefix(editor);
			if (c == '"' || c == ':' || c == '{' || c == '[' || completionPrefix.Length >= 1)
			{
				editor.RequestCodeCompletion(true);
			}
		}
	}

	private void OnCodeCompletionRequested(CodeEdit? editor, JsonEditorKind kind)
	{
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		if (editor == null)
		{
			return;
		}
		bool isInsideString = IsCaretInsideString(editor);
		string prefix = GetCompletionPrefix(editor);
		foreach (CompletionItem item in from c in BuildCompletionItems(kind, isInsideString)
			where string.IsNullOrEmpty(prefix) || c.MatchText.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
			group c by (DisplayText: c.DisplayText, InsertText: c.InsertText) into @group
			select @group.First())
		{
			editor.AddCodeCompletionOption(item.KindValue, item.DisplayText, item.InsertText, (Color?)null, (Resource)null, default(Variant), 1024);
		}
		editor.UpdateCodeCompletionOptions(true);
	}

	private void RegisterEditableInput(Control? control)
	{
		Control control2 = control;
		if (control2 != null)
		{
			control2.FocusEntered += delegate
			{
				lastActiveTextInput = control2;
			};
		}
	}

	private void OnVariantSelected(long index)
	{
		if (isRefreshingUi)
		{
			return;
		}
		int num = (int)index;
		if (num == selectedVariantIndex)
		{
			return;
		}
		if (!TryCommitSelectedVariant(validateCondition: false, out string error))
		{
			ShowMessage(LocalizeText("ui.editor.message.fix_before_switch", "Fix the current variant before switching.\n\n{0}", error));
			isRefreshingUi = true;
			try
			{
				if (variantList != null && selectedVariantIndex >= 0 && selectedVariantIndex < draftDefinitions.Count)
				{
					variantList.Select(selectedVariantIndex, true);
				}
				return;
			}
			finally
			{
				isRefreshingUi = false;
			}
		}
		selectedVariantIndex = num;
		isRefreshingUi = true;
		try
		{
			LoadSelectedDefinitionIntoEditors();
			UpdateActionState();
		}
		finally
		{
			isRefreshingUi = false;
		}
		RefreshPreview();
	}

	private void OnAddVariantPressed()
	{
		if (!TryCommitSelectedVariant(validateCondition: false, out string error))
		{
			ShowMessage(LocalizeText("ui.editor.message.fix_before_add", "Fix the current variant before adding another.\n\n{0}", error));
			return;
		}
		draftDefinitions.Add(new IntentDefinition());
		selectedVariantIndex = draftDefinitions.Count - 1;
		hasUnsavedChanges = true;
		RefreshUiFromState();
	}

	private void OnStateIdClicked(long index, Vector2 atPosition, long mouseButtonIndex)
	{
		if (mouseButtonIndex == 1 && index >= 0 && index < availableStateIds.Count)
		{
			InsertStateIdIntoActiveInput(availableStateIds[(int)index]);
		}
	}

	private void OnDuplicateVariantPressed()
	{
		if (selectedVariantIndex >= 0 && selectedVariantIndex < draftDefinitions.Count)
		{
			if (!TryCommitSelectedVariant(validateCondition: false, out string error))
			{
				ShowMessage(LocalizeText("ui.editor.message.fix_before_duplicate", "Fix the current variant before duplicating it.\n\n{0}", error));
				return;
			}
			IntentDefinition item = IntentDefinitionEditorService.Clone(draftDefinitions[selectedVariantIndex]) ?? new IntentDefinition();
			draftDefinitions.Insert(selectedVariantIndex + 1, item);
			selectedVariantIndex++;
			hasUnsavedChanges = true;
			RefreshUiFromState();
		}
	}

	private void OnDeleteVariantPressed()
	{
		if (selectedVariantIndex >= 0 && selectedVariantIndex < draftDefinitions.Count)
		{
			draftDefinitions.RemoveAt(selectedVariantIndex);
			if (draftDefinitions.Count == 0)
			{
				selectedVariantIndex = -1;
			}
			else if (selectedVariantIndex >= draftDefinitions.Count)
			{
				selectedVariantIndex = draftDefinitions.Count - 1;
			}
			hasUnsavedChanges = true;
			RefreshUiFromState();
		}
	}

	private void OnMoveVariantUpPressed()
	{
		MoveSelectedVariant(-1);
	}

	private void OnMoveVariantDownPressed()
	{
		MoveSelectedVariant(1);
	}

	private void MoveSelectedVariant(int delta)
	{
		if (selectedVariantIndex < 0 || selectedVariantIndex >= draftDefinitions.Count)
		{
			return;
		}
		if (!TryCommitSelectedVariant(validateCondition: false, out string error))
		{
			ShowMessage(LocalizeText("ui.editor.message.fix_before_reorder", "Fix the current variant before reordering.\n\n{0}", error));
			return;
		}
		int num = selectedVariantIndex + delta;
		if (num >= 0 && num < draftDefinitions.Count)
		{
			IntentDefinitionList intentDefinitionList = draftDefinitions;
			int index = selectedVariantIndex;
			IntentDefinitionList intentDefinitionList2 = draftDefinitions;
			int index2 = num;
			IntentDefinition value = draftDefinitions[num];
			IntentDefinition value2 = draftDefinitions[selectedVariantIndex];
			intentDefinitionList[index] = value;
			intentDefinitionList2[index2] = value2;
			selectedVariantIndex = num;
			hasUnsavedChanges = true;
			RefreshUiFromState();
		}
	}

	private void OnSavePressed()
	{
		if (!TryCommitSelectedVariant(validateCondition: true, out string error))
		{
			ShowMessage(LocalizeText("ui.editor.message.save_invalid_current", "Cannot save until the current variant is valid.\n\n{0}", error));
			return;
		}
		if (!ValidateAllVariantConditions(out error))
		{
			ShowMessage(LocalizeText("ui.editor.message.save_invalid_all_conditions", "Cannot save until every variant has a valid condition.\n\n{0}", error));
			return;
		}
		if (!TryCommitIntentStrings(out error))
		{
			ShowMessage(LocalizeText("ui.editor.message.save_invalid_intent_strings", "Cannot save until the current language intent strings JSON is valid.\n\n{0}", error));
			return;
		}
		IntentDefinitionEditorService.SaveEditableDefinitions(monsterModelFullName, draftDefinitions);
		IntentGraphMod.SaveIntentStringsToFile(IntentGraphMod.GetDevIntentStringFilePath(loadedIntentStringsLanguage), draftIntentStrings);
		IntentGraphMod.ReloadIntentDefinitionsAndGraphs();
		hasUnsavedChanges = false;
		UpdateHeaderStatus(LocalizeText("ui.editor.status.saved", "Saved definitions and {0} intent strings.", loadedIntentStringsLanguage));
		RefreshUiFromState();
	}

	private void OnReloadPressed()
	{
		if (hasUnsavedChanges)
		{
			pendingAction = PendingAction.Reload;
			ConfirmationDialog? obj = unsavedChangesDialog;
			if (obj != null)
			{
				((Window)obj).PopupCentered((Vector2I?)null);
			}
		}
		else
		{
			ReloadDefinitions();
		}
	}

	private void OnClosePressed()
	{
		RequestClose();
	}

	private void RequestClose()
	{
		if (hasUnsavedChanges)
		{
			pendingAction = PendingAction.Close;
			ConfirmationDialog? obj = unsavedChangesDialog;
			if (obj != null)
			{
				((Window)obj).PopupCentered((Vector2I?)null);
			}
		}
		else
		{
			CloseEditor();
		}
	}

	private void OnUnsavedChangesConfirmed()
	{
		switch (pendingAction)
		{
		case PendingAction.Close:
			CloseEditor();
			break;
		case PendingAction.Reload:
			ReloadDefinitions();
			break;
		}
		pendingAction = PendingAction.None;
	}

	private void ReloadDefinitions()
	{
		LoadDefinitionsFromDiskOrRuntime();
	}

	private void CloseEditor()
	{
		IntentGraphEditorHost.CloseEditor(this);
	}

	private void RefreshPreview()
	{
		if (previewGraph == null || previewStatusLabel == null)
		{
			return;
		}
		if (monster == null)
		{
			previewGraph.Graph = null;
			previewStatusLabel.Text = LocalizeText("ui.editor.preview.no_monster", "No monster selected.");
			return;
		}
		if (!TryReadIntentStringsFromEditor(out Dictionary<string, string> intentStrings, out string error))
		{
			previewGraph.Graph = null;
			previewStatusLabel.Text = LocalizeText("ui.editor.preview.blocked", "Preview blocked: {0}", error);
			return;
		}
		if (selectedVariantIndex < 0 || selectedVariantIndex >= draftDefinitions.Count)
		{
			previewGraph.Graph = IntentGraphGenerator.GenerateGraph(monster, null, intentStrings);
			previewStatusLabel.Text = ((previewGraph.Graph == null) ? LocalizeText("ui.editor.preview.no_graph_current_monster", "Preview generated no graph for the current monster.") : LocalizeText("ui.editor.preview.updated_runtime", "Preview updated from current runtime graph."));
			UpdateConditionStatus(null);
			UpdateUpToDateConditionStatus(null);
			return;
		}
		if (!TryCommitSelectedVariant(validateCondition: false, out string error2))
		{
			previewStatusLabel.Text = LocalizeText("ui.editor.preview.blocked", "Preview blocked: {0}", error2);
			return;
		}
		IntentDefinition intentDefinition = draftDefinitions[selectedVariantIndex];
		UpdateConditionStatus(intentDefinition.Condition);
		UpdateUpToDateConditionStatus(intentDefinition.UpToDateCondition);
		try
		{
			previewGraph.Graph = IntentGraphGenerator.GenerateGraph(monster, intentDefinition, intentStrings);
			previewStatusLabel.Text = ((previewGraph.Graph == null) ? LocalizeText("ui.editor.preview.no_graph_draft", "Preview generated no graph for this draft.") : LocalizeText("ui.editor.preview.updated", "Preview updated."));
		}
		catch (Exception ex)
		{
			previewGraph.Graph = null;
			previewStatusLabel.Text = LocalizeText("ui.editor.preview.error", "Preview error: {0}", ex.Message);
		}
	}

	private bool TryCommitSelectedVariant(bool validateCondition, out string error)
	{
		error = string.Empty;
		if (selectedVariantIndex < 0 || selectedVariantIndex >= draftDefinitions.Count)
		{
			return true;
		}
		IntentDefinition intentDefinition = IntentDefinitionEditorService.Clone(draftDefinitions[selectedVariantIndex]) ?? new IntentDefinition();
		LineEdit? obj = conditionEdit;
		intentDefinition.Condition = NormalizeConditionText((obj != null) ? obj.Text : null);
		LineEdit? obj2 = upToDateConditionEdit;
		intentDefinition.UpToDateCondition = NormalizeOptionalExpressionText((obj2 != null) ? obj2.Text : null);
		LineEdit? obj3 = offsetXEdit;
		if (!TryParsePositionComponent((obj3 != null) ? obj3.Text : null, LocalizeText("ui.editor.field.offset_x", "offsetX"), out var value, out error))
		{
			return false;
		}
		LineEdit? obj4 = offsetYEdit;
		if (!TryParsePositionComponent((obj4 != null) ? obj4.Text : null, LocalizeText("ui.editor.field.offset_y", "offsetY"), out var value2, out error))
		{
			return false;
		}
		CodeEdit? obj5 = secondaryInitialStatesEdit;
		if (!TryDeserializeField<SecondaryInitialState[]>((obj5 != null) ? ((TextEdit)obj5).Text : null, LocalizeText("ui.editor.field.secondary_initial_states", "secondaryInitialStates"), out SecondaryInitialState[] value3, out error))
		{
			return false;
		}
		CodeEdit? obj6 = stateMachineEdit;
		if (!TryDeserializeField<StateMachineNode[]>((obj6 != null) ? ((TextEdit)obj6).Text : null, LocalizeText("ui.editor.field.state_machine", "stateMachine"), out StateMachineNode[] value4, out error))
		{
			return false;
		}
		CodeEdit? obj7 = moveReplacementsEdit;
		if (!TryDeserializeField<Dictionary<string, MoveReplacement>>((obj7 != null) ? ((TextEdit)obj7).Text : null, LocalizeText("ui.editor.field.move_replacements", "moveReplacements"), out Dictionary<string, MoveReplacement> value5, out error))
		{
			return false;
		}
		CodeEdit? obj8 = graphPatchEdit;
		if (!TryDeserializeField<Graph>((obj8 != null) ? ((TextEdit)obj8).Text : null, LocalizeText("ui.editor.field.graph_patch", "graphPatch"), out Graph value6, out error))
		{
			return false;
		}
		intentDefinition.SecondaryInitialStates = value3;
		intentDefinition.StateMachine = value4;
		intentDefinition.MoveReplacements = value5;
		intentDefinition.GraphPatch = NormalizeGraphPatch(value6);
		intentDefinition.Offset = new Position(value, value2);
		if (validateCondition && !ValidateVariantExpressions(intentDefinition, out error))
		{
			return false;
		}
		draftDefinitions[selectedVariantIndex] = intentDefinition;
		return true;
	}

	private bool TryCommitIntentStrings(out string error)
	{
		if (!TryReadIntentStringsFromEditor(out Dictionary<string, string> intentStrings, out error))
		{
			return false;
		}
		draftIntentStrings = intentStrings;
		return true;
	}

	private bool ValidateAllVariantConditions(out string error)
	{
		for (int i = 0; i < draftDefinitions.Count; i++)
		{
			if (!ValidateVariantExpressions(draftDefinitions[i], out error))
			{
				error = $"Variant #{i + 1}: {error}";
				return false;
			}
		}
		error = string.Empty;
		return true;
	}

	private bool ValidateVariantExpressions(IntentDefinition definition, out string error)
	{
		if (!ValidateCondition(definition.Condition, out error))
		{
			error = LocalizeText("ui.editor.error.rule_field", "{0}: {1}", LocalizeText("ui.editor.field.condition", "condition"), error);
			return false;
		}
		if (!ValidateUpToDateCondition(definition.UpToDateCondition, out error))
		{
			error = LocalizeText("ui.editor.error.rule_field", "{0}: {1}", LocalizeText("ui.editor.field.up_to_date_condition", "upToDateCondition"), error);
			return false;
		}
		return true;
	}

	private bool ValidateCondition(string condition, out string error)
	{
		return ValidateExpression(NormalizeConditionText(condition), LocalizeText("ui.editor.field.condition", "condition"), out error);
	}

	private bool ValidateUpToDateCondition(string? condition, out string error)
	{
		string text = NormalizeOptionalExpressionText(condition);
		if (text == null)
		{
			error = string.Empty;
			return true;
		}
		return ValidateExpression(text, LocalizeText("ui.editor.field.up_to_date_condition", "upToDateCondition"), out error);
	}

	private bool ValidateExpression(string condition, string fieldName, out string error)
	{
		error = string.Empty;
		if (monster == null)
		{
			return true;
		}
		try
		{
			if (IExpression.Parse(condition, new VariableContext(monster)) == null)
			{
				error = LocalizeText("ui.editor.rule.parse_error", "{0} could not be parsed.", fieldName);
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private void UpdateConditionStatus(string? condition)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		if (conditionStatusLabel != null)
		{
			string error;
			if (string.IsNullOrWhiteSpace(condition))
			{
				conditionStatusLabel.Text = LocalizeText("ui.editor.condition.defaults_true", "Condition defaults to true.");
				((CanvasItem)conditionStatusLabel).Modulate = Colors.White;
			}
			else if (ValidateCondition(condition, out error))
			{
				conditionStatusLabel.Text = LocalizeText("ui.editor.condition.valid", "Condition syntax is valid.");
				((CanvasItem)conditionStatusLabel).Modulate = new Color(0.7f, 0.95f, 0.75f, 1f);
			}
			else
			{
				conditionStatusLabel.Text = LocalizeText("ui.editor.condition.warning", "Condition warning: {0}", error);
				((CanvasItem)conditionStatusLabel).Modulate = new Color(1f, 0.7f, 0.65f, 1f);
			}
		}
	}

	private void UpdateUpToDateConditionStatus(string? condition)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		if (upToDateConditionStatusLabel != null)
		{
			string error;
			if (string.IsNullOrWhiteSpace(condition))
			{
				upToDateConditionStatusLabel.Text = LocalizeText("ui.editor.up_to_date_condition.defaults_empty", "No up-to-date check. Outdated warnings are disabled.");
				((CanvasItem)upToDateConditionStatusLabel).Modulate = Colors.White;
			}
			else if (ValidateUpToDateCondition(condition, out error))
			{
				upToDateConditionStatusLabel.Text = LocalizeText("ui.editor.up_to_date_condition.valid", "Up-to-date condition syntax is valid.");
				((CanvasItem)upToDateConditionStatusLabel).Modulate = new Color(0.7f, 0.95f, 0.75f, 1f);
			}
			else
			{
				upToDateConditionStatusLabel.Text = LocalizeText("ui.editor.up_to_date_condition.warning", "Up-to-date condition warning: {0}", error);
				((CanvasItem)upToDateConditionStatusLabel).Modulate = new Color(1f, 0.7f, 0.65f, 1f);
			}
		}
	}

	private void UpdateHeaderStatus(string status)
	{
		if (headerStatusLabel != null)
		{
			headerStatusLabel.Text = status;
		}
	}

	private void UpdateIntentStringsStatus(string? error)
	{
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		if (intentStringsStatusLabel != null)
		{
			string devIntentStringFilePath = IntentGraphMod.GetDevIntentStringFilePath(loadedIntentStringsLanguage);
			if (string.IsNullOrWhiteSpace(error))
			{
				intentStringsStatusLabel.Text = LocalizeText("ui.editor.intent_strings.status.saved_to", "Editing dev intent strings for '{0}'. Saved to {1}.", loadedIntentStringsLanguage, devIntentStringFilePath);
				((CanvasItem)intentStringsStatusLabel).Modulate = Colors.White;
			}
			else
			{
				intentStringsStatusLabel.Text = LocalizeText("ui.editor.intent_strings.status.warning", "Intent strings warning: {0}", error);
				((CanvasItem)intentStringsStatusLabel).Modulate = new Color(1f, 0.7f, 0.65f, 1f);
			}
		}
	}

	private void InsertStateIdIntoActiveInput(string stateId)
	{
		Control val = lastActiveTextInput;
		LineEdit val2 = (LineEdit)(object)((val is LineEdit) ? val : null);
		if (val2 == null)
		{
			TextEdit val3 = (TextEdit)(object)((val is TextEdit) ? val : null);
			if (val3 == null || !val3.Editable)
			{
				goto IL_0065;
			}
			val3.DeleteSelection(-1);
			val3.InsertTextAtCaret(stateId, -1);
			((Control)val3).GrabFocus();
		}
		else
		{
			if (!val2.Editable)
			{
				goto IL_0065;
			}
			val2.DeleteText(val2.GetSelectionFromColumn(), val2.GetSelectionToColumn());
			val2.InsertTextAtCaret(stateId);
			((Control)val2).GrabFocus();
		}
		UpdateHeaderStatus(LocalizeText("ui.editor.status.inserted_state_id", "Inserted state ID '{0}'.", stateId));
		OnEditorFieldsChanged();
		return;
		IL_0065:
		UpdateHeaderStatus(LocalizeText("ui.editor.status.click_editable_first", "Click in an editable text box first, then choose a state ID."));
	}

	private IEnumerable<CompletionItem> BuildCompletionItems(JsonEditorKind kind, bool isInsideString)
	{
		return kind switch
		{
			JsonEditorKind.SecondaryInitialStates => BuildSecondaryInitialStatesCompletionItems(isInsideString), 
			JsonEditorKind.StateMachine => BuildStateMachineCompletionItems(isInsideString), 
			JsonEditorKind.MoveReplacements => BuildMoveReplacementCompletionItems(isInsideString), 
			JsonEditorKind.GraphPatch => BuildGraphPatchCompletionItems(isInsideString), 
			_ => Enumerable.Empty<CompletionItem>(), 
		};
	}

	[IteratorStateMachine(typeof(_003CBuildSecondaryInitialStatesCompletionItems_003Ed__101))]
	private IEnumerable<CompletionItem> BuildSecondaryInitialStatesCompletionItems(bool isInsideString)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CBuildSecondaryInitialStatesCompletionItems_003Ed__101(-2)
		{
			_003C_003E4__this = this,
			_003C_003E3__isInsideString = isInsideString
		};
	}

	[IteratorStateMachine(typeof(_003CBuildStateMachineCompletionItems_003Ed__102))]
	private IEnumerable<CompletionItem> BuildStateMachineCompletionItems(bool isInsideString)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CBuildStateMachineCompletionItems_003Ed__102(-2)
		{
			_003C_003E4__this = this,
			_003C_003E3__isInsideString = isInsideString
		};
	}

	[IteratorStateMachine(typeof(_003CBuildMoveReplacementCompletionItems_003Ed__103))]
	private IEnumerable<CompletionItem> BuildMoveReplacementCompletionItems(bool isInsideString)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CBuildMoveReplacementCompletionItems_003Ed__103(-2)
		{
			_003C_003E4__this = this,
			_003C_003E3__isInsideString = isInsideString
		};
	}

	[IteratorStateMachine(typeof(_003CBuildGraphPatchCompletionItems_003Ed__104))]
	private IEnumerable<CompletionItem> BuildGraphPatchCompletionItems(bool isInsideString)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CBuildGraphPatchCompletionItems_003Ed__104(-2)
		{
			_003C_003E4__this = this,
			_003C_003E3__isInsideString = isInsideString
		};
	}

	private static CompletionItem PropertyCompletion(string propertyName, bool isInsideString, CodeCompletionKind kindValue = 4L)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		if (!isInsideString)
		{
			return new CompletionItem(propertyName, "\"" + propertyName + "\"", "\"" + propertyName + "\": ", kindValue);
		}
		return new CompletionItem(propertyName, propertyName, propertyName, kindValue);
	}

	private static CompletionItem StringValueCompletion(string value, bool isInsideString, CodeCompletionKind kindValue)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		if (!isInsideString)
		{
			return new CompletionItem(value, "\"" + value + "\"", "\"" + value + "\"", kindValue);
		}
		return new CompletionItem(value, value, value, kindValue);
	}

	private static CompletionItem BooleanCompletion(bool value)
	{
		string obj = (value ? "true" : "false");
		return new CompletionItem(obj, obj, obj, (CodeCompletionKind)6);
	}

	private static CompletionItem SnippetCompletion(string displayText, string insertText)
	{
		return new CompletionItem(displayText, displayText, insertText, (CodeCompletionKind)9);
	}

	private static CompletionItem PlainTextCompletion(string displayText, string insertText)
	{
		return new CompletionItem(displayText, displayText, insertText, (CodeCompletionKind)9);
	}

	private static string GetCompletionPrefix(CodeEdit editor)
	{
		string line = ((TextEdit)editor).GetLine(((TextEdit)editor).GetCaretLine(0));
		int num = Math.Min(((TextEdit)editor).GetCaretColumn(0), line.Length);
		int num2 = num;
		while (num2 > 0 && IsCompletionCharacter(line[num2 - 1]))
		{
			num2--;
		}
		return line.Substring(num2, num - num2);
	}

	private static bool IsCompletionCharacter(char character)
	{
		if (!char.IsLetterOrDigit(character) && character != '_')
		{
			return character == '-';
		}
		return true;
	}

	private static bool IsCaretInsideString(CodeEdit editor)
	{
		int caretLine = ((TextEdit)editor).GetCaretLine(0);
		int caretColumn = ((TextEdit)editor).GetCaretColumn(0);
		if (editor.IsInString(caretLine, caretColumn) < 0)
		{
			if (caretColumn > 0)
			{
				return editor.IsInString(caretLine, caretColumn - 1) >= 0;
			}
			return false;
		}
		return true;
	}

	private void ShowMessage(string text)
	{
		if (messageDialog != null)
		{
			messageDialog.DialogText = text;
			((Window)messageDialog).PopupCentered((Vector2I?)null);
		}
	}

	private static Graph? NormalizeGraphPatch(Graph? graphPatch)
	{
		if (graphPatch == null)
		{
			return null;
		}
		if (graphPatch.Width == 1f && graphPatch.Height == 1f && graphPatch.Arrows.Count == 0 && graphPatch.Moves.Count == 0 && graphPatch.Icons.Count == 0 && graphPatch.IconGroups.Count == 0 && graphPatch.Labels.Count == 0)
		{
			return null;
		}
		return graphPatch;
	}

	private static string[]? NormalizeSecondaryInitialStates(string[]? secondaryInitialStates)
	{
		if (secondaryInitialStates == null)
		{
			return null;
		}
		string[] array = (from stateId in secondaryInitialStates
			where !string.IsNullOrWhiteSpace(stateId)
			select stateId.Trim()).Distinct<string>(StringComparer.Ordinal).ToArray();
		if (array.Length != 0)
		{
			return array;
		}
		return null;
	}

	private static string NormalizeConditionText(string? condition)
	{
		return NormalizeOptionalExpressionText(condition) ?? "true";
	}

	private static string FormatPositionComponent(float value)
	{
		if (value != 0f)
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}
		return string.Empty;
	}

	private static string? NormalizeOptionalExpressionText(string? condition)
	{
		if (!string.IsNullOrWhiteSpace(condition))
		{
			return condition.Trim();
		}
		return null;
	}

	private bool TryParsePositionComponent(string? text, string fieldName, out float value, out string error)
	{
		error = string.Empty;
		value = 0f;
		if (string.IsNullOrWhiteSpace(text))
		{
			return true;
		}
		if (float.TryParse(text.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value))
		{
			return true;
		}
		error = LocalizeText("ui.editor.error.invalid_number", "Invalid {0}: expected a number using '.' for decimals.", fieldName);
		return false;
	}

	private bool TryDeserializeField<T>(string? text, string fieldName, out T? value, out string error)
	{
		error = string.Empty;
		value = default(T);
		if (string.IsNullOrWhiteSpace(text))
		{
			return true;
		}
		try
		{
			value = IntentDefinitionEditorService.DeserializeJson<T>(text);
			return true;
		}
		catch (Exception ex)
		{
			error = LocalizeText("ui.editor.error.invalid_json", "Invalid {0} JSON: {1}", fieldName, ex.Message);
			return false;
		}
	}

	private bool TryReadIntentStringsFromEditor(out Dictionary<string, string> intentStrings, out string error)
	{
		intentStrings = new Dictionary<string, string>(StringComparer.Ordinal);
		CodeEdit? obj = intentStringsEdit;
		if (string.IsNullOrWhiteSpace((obj != null) ? ((TextEdit)obj).Text : null))
		{
			error = string.Empty;
			return true;
		}
		if (!TryDeserializeField<Dictionary<string, string>>(((TextEdit)intentStringsEdit).Text, LocalizeText("ui.editor.field.intent_strings", "intent strings"), out Dictionary<string, string> value, out error))
		{
			return false;
		}
		intentStrings = value ?? new Dictionary<string, string>(StringComparer.Ordinal);
		return true;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Expected O, but got Unknown
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_031d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0343: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		//IL_0371: Unknown result type (might be due to invalid IL or missing references)
		//IL_0397: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_041d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0428: Expected O, but got Unknown
		//IL_0423: Unknown result type (might be due to invalid IL or missing references)
		//IL_0444: Unknown result type (might be due to invalid IL or missing references)
		//IL_044f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0475: Unknown result type (might be due to invalid IL or missing references)
		//IL_049d: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a8: Expected O, but got Unknown
		//IL_04a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0507: Expected O, but got Unknown
		//IL_0502: Unknown result type (might be due to invalid IL or missing references)
		//IL_0523: Unknown result type (might be due to invalid IL or missing references)
		//IL_052e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0554: Unknown result type (might be due to invalid IL or missing references)
		//IL_057c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0587: Expected O, but got Unknown
		//IL_0582: Unknown result type (might be due to invalid IL or missing references)
		//IL_058d: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0607: Unknown result type (might be due to invalid IL or missing references)
		//IL_0610: Unknown result type (might be due to invalid IL or missing references)
		//IL_0636: Unknown result type (might be due to invalid IL or missing references)
		//IL_0659: Unknown result type (might be due to invalid IL or missing references)
		//IL_067a: Unknown result type (might be due to invalid IL or missing references)
		//IL_069b: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_06cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_06fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0704: Unknown result type (might be due to invalid IL or missing references)
		//IL_072a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0733: Unknown result type (might be due to invalid IL or missing references)
		//IL_0759: Unknown result type (might be due to invalid IL or missing references)
		//IL_0762: Unknown result type (might be due to invalid IL or missing references)
		//IL_0788: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_07dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_080b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0814: Unknown result type (might be due to invalid IL or missing references)
		//IL_083a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0843: Unknown result type (might be due to invalid IL or missing references)
		//IL_0869: Unknown result type (might be due to invalid IL or missing references)
		//IL_0872: Unknown result type (might be due to invalid IL or missing references)
		//IL_0898: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_08d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0925: Unknown result type (might be due to invalid IL or missing references)
		//IL_092e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0954: Unknown result type (might be due to invalid IL or missing references)
		//IL_0977: Unknown result type (might be due to invalid IL or missing references)
		//IL_0982: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_09cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_09fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a1f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a2a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a50: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a73: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a7e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aa4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ac7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ad2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0af8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b21: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b2c: Expected O, but got Unknown
		//IL_0b27: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b32: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b58: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b7c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b87: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bad: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bd6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0be1: Expected O, but got Unknown
		//IL_0bdc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0be7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c0d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c30: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c3b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c62: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c87: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c92: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cb8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cdc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ce7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d0d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d31: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d3c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d62: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d86: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d91: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(49)
		{
			new MethodInfo(MethodName.LocalizeText, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("key"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)4, StringName.op_Implicit("fallback"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._UnhandledInput, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("event"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.ApplyViewportLayout, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.LoadDefinitionsFromDiskOrRuntime, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ApplyLocalization, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetTabTitle, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("childName"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)4, StringName.op_Implicit("title"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.RefreshUiFromState, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.RefreshVariantList, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.RefreshStateIdList, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.LoadSelectedDefinitionIntoEditors, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.LoadIntentStringsIntoEditor, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateActionState, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetEditorsEnabled, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("enabled"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnEditorFieldsChanged, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnIntentStringsChanged, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ConfigureCodeEditor, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("editor"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("CodeEdit"), false),
				new PropertyInfo((Type)1, StringName.op_Implicit("enableCompletion"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnCodeEditorTextChanged, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("editor"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("CodeEdit"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnCodeCompletionRequested, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("editor"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("CodeEdit"), false),
				new PropertyInfo((Type)2, StringName.op_Implicit("kind"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.RegisterEditableInput, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("control"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnVariantSelected, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("index"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnAddVariantPressed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnStateIdClicked, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("index"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)5, StringName.op_Implicit("atPosition"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)2, StringName.op_Implicit("mouseButtonIndex"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnDuplicateVariantPressed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnDeleteVariantPressed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnMoveVariantUpPressed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnMoveVariantDownPressed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.MoveSelectedVariant, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnSavePressed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnReloadPressed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnClosePressed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.RequestClose, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnUnsavedChangesConfirmed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ReloadDefinitions, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.CloseEditor, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.RefreshPreview, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateConditionStatus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("condition"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateUpToDateConditionStatus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("condition"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateHeaderStatus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("status"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateIntentStringsStatus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("error"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.InsertStateIdIntoActiveInput, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("stateId"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.GetCompletionPrefix, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("editor"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("CodeEdit"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.IsCompletionCharacter, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("character"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.IsCaretInsideString, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("editor"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("CodeEdit"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.ShowMessage, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("text"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.NormalizeSecondaryInitialStates, new PropertyInfo((Type)34, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)34, StringName.op_Implicit("secondaryInitialStates"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.NormalizeConditionText, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("condition"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.FormatPositionComponent, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("value"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.NormalizeOptionalExpressionText, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("condition"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0413: Unknown result type (might be due to invalid IL or missing references)
		//IL_042b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0450: Unknown result type (might be due to invalid IL or missing references)
		//IL_0475: Unknown result type (might be due to invalid IL or missing references)
		//IL_049a: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0517: Unknown result type (might be due to invalid IL or missing references)
		//IL_053c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0561: Unknown result type (might be due to invalid IL or missing references)
		//IL_0586: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_061a: Unknown result type (might be due to invalid IL or missing references)
		//IL_064d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0680: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0719: Unknown result type (might be due to invalid IL or missing references)
		//IL_074e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0753: Unknown result type (might be due to invalid IL or missing references)
		//IL_0787: Unknown result type (might be due to invalid IL or missing references)
		//IL_078c: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_082d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0832: Unknown result type (might be due to invalid IL or missing references)
		//IL_0867: Unknown result type (might be due to invalid IL or missing references)
		//IL_086c: Unknown result type (might be due to invalid IL or missing references)
		//IL_08e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_08db: Unknown result type (might be due to invalid IL or missing references)
		//IL_08e0: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.LocalizeText && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			string text = LocalizeText(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<string>(ref text);
			return true;
		}
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._UnhandledInput && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._UnhandledInput(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ApplyViewportLayout && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ApplyViewportLayout();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.LoadDefinitionsFromDiskOrRuntime && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			LoadDefinitionsFromDiskOrRuntime();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ApplyLocalization && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ApplyLocalization();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetTabTitle && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			SetTabTitle(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RefreshUiFromState && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RefreshUiFromState();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RefreshVariantList && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RefreshVariantList();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RefreshStateIdList && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RefreshStateIdList();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.LoadSelectedDefinitionIntoEditors && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			LoadSelectedDefinitionIntoEditors();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.LoadIntentStringsIntoEditor && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			LoadIntentStringsIntoEditor();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateActionState && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			UpdateActionState();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetEditorsEnabled && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetEditorsEnabled(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnEditorFieldsChanged && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnEditorFieldsChanged();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnIntentStringsChanged && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnIntentStringsChanged();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ConfigureCodeEditor && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			ConfigureCodeEditor(VariantUtils.ConvertTo<CodeEdit>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnCodeEditorTextChanged && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnCodeEditorTextChanged(VariantUtils.ConvertTo<CodeEdit>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnCodeCompletionRequested && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			OnCodeCompletionRequested(VariantUtils.ConvertTo<CodeEdit>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<JsonEditorKind>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RegisterEditableInput && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			RegisterEditableInput(VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnVariantSelected && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnVariantSelected(VariantUtils.ConvertTo<long>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnAddVariantPressed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnAddVariantPressed();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnStateIdClicked && ((NativeVariantPtrArgs)(ref args)).Count == 3)
		{
			OnStateIdClicked(VariantUtils.ConvertTo<long>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<long>(ref ((NativeVariantPtrArgs)(ref args))[2]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnDuplicateVariantPressed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnDuplicateVariantPressed();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnDeleteVariantPressed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnDeleteVariantPressed();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnMoveVariantUpPressed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnMoveVariantUpPressed();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnMoveVariantDownPressed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnMoveVariantDownPressed();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.MoveSelectedVariant && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			MoveSelectedVariant(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnSavePressed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnSavePressed();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnReloadPressed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnReloadPressed();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnClosePressed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnClosePressed();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RequestClose && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RequestClose();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnUnsavedChangesConfirmed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnUnsavedChangesConfirmed();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ReloadDefinitions && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ReloadDefinitions();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CloseEditor && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			CloseEditor();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RefreshPreview && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RefreshPreview();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateConditionStatus && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			UpdateConditionStatus(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateUpToDateConditionStatus && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			UpdateUpToDateConditionStatus(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateHeaderStatus && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			UpdateHeaderStatus(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateIntentStringsStatus && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			UpdateIntentStringsStatus(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.InsertStateIdIntoActiveInput && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			InsertStateIdIntoActiveInput(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.GetCompletionPrefix && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			string completionPrefix = GetCompletionPrefix(VariantUtils.ConvertTo<CodeEdit>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<string>(ref completionPrefix);
			return true;
		}
		if ((ref method) == MethodName.IsCompletionCharacter && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag = IsCompletionCharacter(VariantUtils.ConvertTo<char>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		if ((ref method) == MethodName.IsCaretInsideString && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag2 = IsCaretInsideString(VariantUtils.ConvertTo<CodeEdit>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag2);
			return true;
		}
		if ((ref method) == MethodName.ShowMessage && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			ShowMessage(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.NormalizeSecondaryInitialStates && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			string[] array = NormalizeSecondaryInitialStates(VariantUtils.ConvertTo<string[]>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<string[]>(ref array);
			return true;
		}
		if ((ref method) == MethodName.NormalizeConditionText && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			string text2 = NormalizeConditionText(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<string>(ref text2);
			return true;
		}
		if ((ref method) == MethodName.FormatPositionComponent && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			string text3 = FormatPositionComponent(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<string>(ref text3);
			return true;
		}
		if ((ref method) == MethodName.NormalizeOptionalExpressionText && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			string text4 = NormalizeOptionalExpressionText(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<string>(ref text4);
			return true;
		}
		return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.LocalizeText && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			string text = LocalizeText(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<string>(ref text);
			return true;
		}
		if ((ref method) == MethodName.GetCompletionPrefix && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			string completionPrefix = GetCompletionPrefix(VariantUtils.ConvertTo<CodeEdit>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<string>(ref completionPrefix);
			return true;
		}
		if ((ref method) == MethodName.IsCompletionCharacter && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag = IsCompletionCharacter(VariantUtils.ConvertTo<char>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		if ((ref method) == MethodName.IsCaretInsideString && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag2 = IsCaretInsideString(VariantUtils.ConvertTo<CodeEdit>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag2);
			return true;
		}
		if ((ref method) == MethodName.NormalizeSecondaryInitialStates && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			string[] array = NormalizeSecondaryInitialStates(VariantUtils.ConvertTo<string[]>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<string[]>(ref array);
			return true;
		}
		if ((ref method) == MethodName.NormalizeConditionText && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			string text2 = NormalizeConditionText(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<string>(ref text2);
			return true;
		}
		if ((ref method) == MethodName.FormatPositionComponent && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			string text3 = FormatPositionComponent(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<string>(ref text3);
			return true;
		}
		if ((ref method) == MethodName.NormalizeOptionalExpressionText && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			string text4 = NormalizeOptionalExpressionText(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<string>(ref text4);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.LocalizeText)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName._UnhandledInput)
		{
			return true;
		}
		if ((ref method) == MethodName.ApplyViewportLayout)
		{
			return true;
		}
		if ((ref method) == MethodName.LoadDefinitionsFromDiskOrRuntime)
		{
			return true;
		}
		if ((ref method) == MethodName.ApplyLocalization)
		{
			return true;
		}
		if ((ref method) == MethodName.SetTabTitle)
		{
			return true;
		}
		if ((ref method) == MethodName.RefreshUiFromState)
		{
			return true;
		}
		if ((ref method) == MethodName.RefreshVariantList)
		{
			return true;
		}
		if ((ref method) == MethodName.RefreshStateIdList)
		{
			return true;
		}
		if ((ref method) == MethodName.LoadSelectedDefinitionIntoEditors)
		{
			return true;
		}
		if ((ref method) == MethodName.LoadIntentStringsIntoEditor)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateActionState)
		{
			return true;
		}
		if ((ref method) == MethodName.SetEditorsEnabled)
		{
			return true;
		}
		if ((ref method) == MethodName.OnEditorFieldsChanged)
		{
			return true;
		}
		if ((ref method) == MethodName.OnIntentStringsChanged)
		{
			return true;
		}
		if ((ref method) == MethodName.ConfigureCodeEditor)
		{
			return true;
		}
		if ((ref method) == MethodName.OnCodeEditorTextChanged)
		{
			return true;
		}
		if ((ref method) == MethodName.OnCodeCompletionRequested)
		{
			return true;
		}
		if ((ref method) == MethodName.RegisterEditableInput)
		{
			return true;
		}
		if ((ref method) == MethodName.OnVariantSelected)
		{
			return true;
		}
		if ((ref method) == MethodName.OnAddVariantPressed)
		{
			return true;
		}
		if ((ref method) == MethodName.OnStateIdClicked)
		{
			return true;
		}
		if ((ref method) == MethodName.OnDuplicateVariantPressed)
		{
			return true;
		}
		if ((ref method) == MethodName.OnDeleteVariantPressed)
		{
			return true;
		}
		if ((ref method) == MethodName.OnMoveVariantUpPressed)
		{
			return true;
		}
		if ((ref method) == MethodName.OnMoveVariantDownPressed)
		{
			return true;
		}
		if ((ref method) == MethodName.MoveSelectedVariant)
		{
			return true;
		}
		if ((ref method) == MethodName.OnSavePressed)
		{
			return true;
		}
		if ((ref method) == MethodName.OnReloadPressed)
		{
			return true;
		}
		if ((ref method) == MethodName.OnClosePressed)
		{
			return true;
		}
		if ((ref method) == MethodName.RequestClose)
		{
			return true;
		}
		if ((ref method) == MethodName.OnUnsavedChangesConfirmed)
		{
			return true;
		}
		if ((ref method) == MethodName.ReloadDefinitions)
		{
			return true;
		}
		if ((ref method) == MethodName.CloseEditor)
		{
			return true;
		}
		if ((ref method) == MethodName.RefreshPreview)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateConditionStatus)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateUpToDateConditionStatus)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateHeaderStatus)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateIntentStringsStatus)
		{
			return true;
		}
		if ((ref method) == MethodName.InsertStateIdIntoActiveInput)
		{
			return true;
		}
		if ((ref method) == MethodName.GetCompletionPrefix)
		{
			return true;
		}
		if ((ref method) == MethodName.IsCompletionCharacter)
		{
			return true;
		}
		if ((ref method) == MethodName.IsCaretInsideString)
		{
			return true;
		}
		if ((ref method) == MethodName.ShowMessage)
		{
			return true;
		}
		if ((ref method) == MethodName.NormalizeSecondaryInitialStates)
		{
			return true;
		}
		if ((ref method) == MethodName.NormalizeConditionText)
		{
			return true;
		}
		if ((ref method) == MethodName.FormatPositionComponent)
		{
			return true;
		}
		if ((ref method) == MethodName.NormalizeOptionalExpressionText)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.monsterNameLabel)
		{
			monsterNameLabel = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.monsterModelLabel)
		{
			monsterModelLabel = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.headerStatusLabel)
		{
			headerStatusLabel = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.variantList)
		{
			variantList = VariantUtils.ConvertTo<ItemList>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.stateIdList)
		{
			stateIdList = VariantUtils.ConvertTo<ItemList>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.addVariantButton)
		{
			addVariantButton = VariantUtils.ConvertTo<Button>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.duplicateVariantButton)
		{
			duplicateVariantButton = VariantUtils.ConvertTo<Button>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.deleteVariantButton)
		{
			deleteVariantButton = VariantUtils.ConvertTo<Button>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.moveVariantUpButton)
		{
			moveVariantUpButton = VariantUtils.ConvertTo<Button>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.moveVariantDownButton)
		{
			moveVariantDownButton = VariantUtils.ConvertTo<Button>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.saveButton)
		{
			saveButton = VariantUtils.ConvertTo<Button>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.reloadButton)
		{
			reloadButton = VariantUtils.ConvertTo<Button>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.closeButton)
		{
			closeButton = VariantUtils.ConvertTo<Button>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.conditionEdit)
		{
			conditionEdit = VariantUtils.ConvertTo<LineEdit>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.conditionStatusLabel)
		{
			conditionStatusLabel = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.upToDateConditionEdit)
		{
			upToDateConditionEdit = VariantUtils.ConvertTo<LineEdit>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.upToDateConditionStatusLabel)
		{
			upToDateConditionStatusLabel = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.offsetXEdit)
		{
			offsetXEdit = VariantUtils.ConvertTo<LineEdit>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.offsetYEdit)
		{
			offsetYEdit = VariantUtils.ConvertTo<LineEdit>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.secondaryInitialStatesEdit)
		{
			secondaryInitialStatesEdit = VariantUtils.ConvertTo<CodeEdit>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.stateMachineEdit)
		{
			stateMachineEdit = VariantUtils.ConvertTo<CodeEdit>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.moveReplacementsEdit)
		{
			moveReplacementsEdit = VariantUtils.ConvertTo<CodeEdit>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.graphPatchEdit)
		{
			graphPatchEdit = VariantUtils.ConvertTo<CodeEdit>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.intentStringsStatusLabel)
		{
			intentStringsStatusLabel = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.intentStringsEdit)
		{
			intentStringsEdit = VariantUtils.ConvertTo<CodeEdit>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.readOnlySummaryEdit)
		{
			readOnlySummaryEdit = VariantUtils.ConvertTo<CodeEdit>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.previewStatusLabel)
		{
			previewStatusLabel = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.previewGraph)
		{
			previewGraph = VariantUtils.ConvertTo<NIntentGraph>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.unsavedChangesDialog)
		{
			unsavedChangesDialog = VariantUtils.ConvertTo<ConfirmationDialog>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.messageDialog)
		{
			messageDialog = VariantUtils.ConvertTo<AcceptDialog>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.windowPanel)
		{
			windowPanel = VariantUtils.ConvertTo<PanelContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.editorTabs)
		{
			editorTabs = VariantUtils.ConvertTo<TabContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.monsterDisplayName)
		{
			monsterDisplayName = VariantUtils.ConvertTo<string>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.monsterModelFullName)
		{
			monsterModelFullName = VariantUtils.ConvertTo<string>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.loadedIntentStringsLanguage)
		{
			loadedIntentStringsLanguage = VariantUtils.ConvertTo<string>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.selectedVariantIndex)
		{
			selectedVariantIndex = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.isReady)
		{
			isReady = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.isRefreshingUi)
		{
			isRefreshingUi = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.hasUnsavedChanges)
		{
			hasUnsavedChanges = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.pendingAction)
		{
			pendingAction = VariantUtils.ConvertTo<PendingAction>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.lastActiveTextInput)
		{
			lastActiveTextInput = VariantUtils.ConvertTo<Control>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0357: Unknown result type (might be due to invalid IL or missing references)
		//IL_035c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0377: Unknown result type (might be due to invalid IL or missing references)
		//IL_037c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0397: Unknown result type (might be due to invalid IL or missing references)
		//IL_039c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0417: Unknown result type (might be due to invalid IL or missing references)
		//IL_041c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0437: Unknown result type (might be due to invalid IL or missing references)
		//IL_043c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0457: Unknown result type (might be due to invalid IL or missing references)
		//IL_045c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0477: Unknown result type (might be due to invalid IL or missing references)
		//IL_047c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0497: Unknown result type (might be due to invalid IL or missing references)
		//IL_049c: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0517: Unknown result type (might be due to invalid IL or missing references)
		//IL_051c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0537: Unknown result type (might be due to invalid IL or missing references)
		//IL_053c: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.HasUnsavedChanges)
		{
			bool flag = HasUnsavedChanges;
			value = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		if ((ref name) == PropertyName.monsterNameLabel)
		{
			value = VariantUtils.CreateFrom<Label>(ref monsterNameLabel);
			return true;
		}
		if ((ref name) == PropertyName.monsterModelLabel)
		{
			value = VariantUtils.CreateFrom<Label>(ref monsterModelLabel);
			return true;
		}
		if ((ref name) == PropertyName.headerStatusLabel)
		{
			value = VariantUtils.CreateFrom<Label>(ref headerStatusLabel);
			return true;
		}
		if ((ref name) == PropertyName.variantList)
		{
			value = VariantUtils.CreateFrom<ItemList>(ref variantList);
			return true;
		}
		if ((ref name) == PropertyName.stateIdList)
		{
			value = VariantUtils.CreateFrom<ItemList>(ref stateIdList);
			return true;
		}
		if ((ref name) == PropertyName.addVariantButton)
		{
			value = VariantUtils.CreateFrom<Button>(ref addVariantButton);
			return true;
		}
		if ((ref name) == PropertyName.duplicateVariantButton)
		{
			value = VariantUtils.CreateFrom<Button>(ref duplicateVariantButton);
			return true;
		}
		if ((ref name) == PropertyName.deleteVariantButton)
		{
			value = VariantUtils.CreateFrom<Button>(ref deleteVariantButton);
			return true;
		}
		if ((ref name) == PropertyName.moveVariantUpButton)
		{
			value = VariantUtils.CreateFrom<Button>(ref moveVariantUpButton);
			return true;
		}
		if ((ref name) == PropertyName.moveVariantDownButton)
		{
			value = VariantUtils.CreateFrom<Button>(ref moveVariantDownButton);
			return true;
		}
		if ((ref name) == PropertyName.saveButton)
		{
			value = VariantUtils.CreateFrom<Button>(ref saveButton);
			return true;
		}
		if ((ref name) == PropertyName.reloadButton)
		{
			value = VariantUtils.CreateFrom<Button>(ref reloadButton);
			return true;
		}
		if ((ref name) == PropertyName.closeButton)
		{
			value = VariantUtils.CreateFrom<Button>(ref closeButton);
			return true;
		}
		if ((ref name) == PropertyName.conditionEdit)
		{
			value = VariantUtils.CreateFrom<LineEdit>(ref conditionEdit);
			return true;
		}
		if ((ref name) == PropertyName.conditionStatusLabel)
		{
			value = VariantUtils.CreateFrom<Label>(ref conditionStatusLabel);
			return true;
		}
		if ((ref name) == PropertyName.upToDateConditionEdit)
		{
			value = VariantUtils.CreateFrom<LineEdit>(ref upToDateConditionEdit);
			return true;
		}
		if ((ref name) == PropertyName.upToDateConditionStatusLabel)
		{
			value = VariantUtils.CreateFrom<Label>(ref upToDateConditionStatusLabel);
			return true;
		}
		if ((ref name) == PropertyName.offsetXEdit)
		{
			value = VariantUtils.CreateFrom<LineEdit>(ref offsetXEdit);
			return true;
		}
		if ((ref name) == PropertyName.offsetYEdit)
		{
			value = VariantUtils.CreateFrom<LineEdit>(ref offsetYEdit);
			return true;
		}
		if ((ref name) == PropertyName.secondaryInitialStatesEdit)
		{
			value = VariantUtils.CreateFrom<CodeEdit>(ref secondaryInitialStatesEdit);
			return true;
		}
		if ((ref name) == PropertyName.stateMachineEdit)
		{
			value = VariantUtils.CreateFrom<CodeEdit>(ref stateMachineEdit);
			return true;
		}
		if ((ref name) == PropertyName.moveReplacementsEdit)
		{
			value = VariantUtils.CreateFrom<CodeEdit>(ref moveReplacementsEdit);
			return true;
		}
		if ((ref name) == PropertyName.graphPatchEdit)
		{
			value = VariantUtils.CreateFrom<CodeEdit>(ref graphPatchEdit);
			return true;
		}
		if ((ref name) == PropertyName.intentStringsStatusLabel)
		{
			value = VariantUtils.CreateFrom<Label>(ref intentStringsStatusLabel);
			return true;
		}
		if ((ref name) == PropertyName.intentStringsEdit)
		{
			value = VariantUtils.CreateFrom<CodeEdit>(ref intentStringsEdit);
			return true;
		}
		if ((ref name) == PropertyName.readOnlySummaryEdit)
		{
			value = VariantUtils.CreateFrom<CodeEdit>(ref readOnlySummaryEdit);
			return true;
		}
		if ((ref name) == PropertyName.previewStatusLabel)
		{
			value = VariantUtils.CreateFrom<Label>(ref previewStatusLabel);
			return true;
		}
		if ((ref name) == PropertyName.previewGraph)
		{
			value = VariantUtils.CreateFrom<NIntentGraph>(ref previewGraph);
			return true;
		}
		if ((ref name) == PropertyName.unsavedChangesDialog)
		{
			value = VariantUtils.CreateFrom<ConfirmationDialog>(ref unsavedChangesDialog);
			return true;
		}
		if ((ref name) == PropertyName.messageDialog)
		{
			value = VariantUtils.CreateFrom<AcceptDialog>(ref messageDialog);
			return true;
		}
		if ((ref name) == PropertyName.windowPanel)
		{
			value = VariantUtils.CreateFrom<PanelContainer>(ref windowPanel);
			return true;
		}
		if ((ref name) == PropertyName.editorTabs)
		{
			value = VariantUtils.CreateFrom<TabContainer>(ref editorTabs);
			return true;
		}
		if ((ref name) == PropertyName.monsterDisplayName)
		{
			value = VariantUtils.CreateFrom<string>(ref monsterDisplayName);
			return true;
		}
		if ((ref name) == PropertyName.monsterModelFullName)
		{
			value = VariantUtils.CreateFrom<string>(ref monsterModelFullName);
			return true;
		}
		if ((ref name) == PropertyName.loadedIntentStringsLanguage)
		{
			value = VariantUtils.CreateFrom<string>(ref loadedIntentStringsLanguage);
			return true;
		}
		if ((ref name) == PropertyName.selectedVariantIndex)
		{
			value = VariantUtils.CreateFrom<int>(ref selectedVariantIndex);
			return true;
		}
		if ((ref name) == PropertyName.isReady)
		{
			value = VariantUtils.CreateFrom<bool>(ref isReady);
			return true;
		}
		if ((ref name) == PropertyName.isRefreshingUi)
		{
			value = VariantUtils.CreateFrom<bool>(ref isRefreshingUi);
			return true;
		}
		if ((ref name) == PropertyName.hasUnsavedChanges)
		{
			value = VariantUtils.CreateFrom<bool>(ref hasUnsavedChanges);
			return true;
		}
		if ((ref name) == PropertyName.pendingAction)
		{
			value = VariantUtils.CreateFrom<PendingAction>(ref pendingAction);
			return true;
		}
		if ((ref name) == PropertyName.lastActiveTextInput)
		{
			value = VariantUtils.CreateFrom<Control>(ref lastActiveTextInput);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Unknown result type (might be due to invalid IL or missing references)
		//IL_0355: Unknown result type (might be due to invalid IL or missing references)
		//IL_0376: Unknown result type (might be due to invalid IL or missing references)
		//IL_0397: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_041b: Unknown result type (might be due to invalid IL or missing references)
		//IL_043b: Unknown result type (might be due to invalid IL or missing references)
		//IL_045b: Unknown result type (might be due to invalid IL or missing references)
		//IL_047b: Unknown result type (might be due to invalid IL or missing references)
		//IL_049b: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_04db: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_051b: Unknown result type (might be due to invalid IL or missing references)
		//IL_053c: Unknown result type (might be due to invalid IL or missing references)
		//IL_055c: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName.monsterNameLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.monsterModelLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.headerStatusLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.variantList, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.stateIdList, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.addVariantButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.duplicateVariantButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.deleteVariantButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.moveVariantUpButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.moveVariantDownButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.saveButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.reloadButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.closeButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.conditionEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.conditionStatusLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.upToDateConditionEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.upToDateConditionStatusLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.offsetXEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.offsetYEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.secondaryInitialStatesEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.stateMachineEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.moveReplacementsEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.graphPatchEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.intentStringsStatusLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.intentStringsEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.readOnlySummaryEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.previewStatusLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.previewGraph, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.unsavedChangesDialog, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.messageDialog, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.windowPanel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.editorTabs, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)4, PropertyName.monsterDisplayName, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)4, PropertyName.monsterModelFullName, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)4, PropertyName.loadedIntentStringsLanguage, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName.selectedVariantIndex, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.isReady, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.isRefreshingUi, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.hasUnsavedChanges, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName.pendingAction, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.lastActiveTextInput, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.HasUnsavedChanges, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_0357: Unknown result type (might be due to invalid IL or missing references)
		//IL_036d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0383: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName.monsterNameLabel, Variant.From<Label>(ref monsterNameLabel));
		info.AddProperty(PropertyName.monsterModelLabel, Variant.From<Label>(ref monsterModelLabel));
		info.AddProperty(PropertyName.headerStatusLabel, Variant.From<Label>(ref headerStatusLabel));
		info.AddProperty(PropertyName.variantList, Variant.From<ItemList>(ref variantList));
		info.AddProperty(PropertyName.stateIdList, Variant.From<ItemList>(ref stateIdList));
		info.AddProperty(PropertyName.addVariantButton, Variant.From<Button>(ref addVariantButton));
		info.AddProperty(PropertyName.duplicateVariantButton, Variant.From<Button>(ref duplicateVariantButton));
		info.AddProperty(PropertyName.deleteVariantButton, Variant.From<Button>(ref deleteVariantButton));
		info.AddProperty(PropertyName.moveVariantUpButton, Variant.From<Button>(ref moveVariantUpButton));
		info.AddProperty(PropertyName.moveVariantDownButton, Variant.From<Button>(ref moveVariantDownButton));
		info.AddProperty(PropertyName.saveButton, Variant.From<Button>(ref saveButton));
		info.AddProperty(PropertyName.reloadButton, Variant.From<Button>(ref reloadButton));
		info.AddProperty(PropertyName.closeButton, Variant.From<Button>(ref closeButton));
		info.AddProperty(PropertyName.conditionEdit, Variant.From<LineEdit>(ref conditionEdit));
		info.AddProperty(PropertyName.conditionStatusLabel, Variant.From<Label>(ref conditionStatusLabel));
		info.AddProperty(PropertyName.upToDateConditionEdit, Variant.From<LineEdit>(ref upToDateConditionEdit));
		info.AddProperty(PropertyName.upToDateConditionStatusLabel, Variant.From<Label>(ref upToDateConditionStatusLabel));
		info.AddProperty(PropertyName.offsetXEdit, Variant.From<LineEdit>(ref offsetXEdit));
		info.AddProperty(PropertyName.offsetYEdit, Variant.From<LineEdit>(ref offsetYEdit));
		info.AddProperty(PropertyName.secondaryInitialStatesEdit, Variant.From<CodeEdit>(ref secondaryInitialStatesEdit));
		info.AddProperty(PropertyName.stateMachineEdit, Variant.From<CodeEdit>(ref stateMachineEdit));
		info.AddProperty(PropertyName.moveReplacementsEdit, Variant.From<CodeEdit>(ref moveReplacementsEdit));
		info.AddProperty(PropertyName.graphPatchEdit, Variant.From<CodeEdit>(ref graphPatchEdit));
		info.AddProperty(PropertyName.intentStringsStatusLabel, Variant.From<Label>(ref intentStringsStatusLabel));
		info.AddProperty(PropertyName.intentStringsEdit, Variant.From<CodeEdit>(ref intentStringsEdit));
		info.AddProperty(PropertyName.readOnlySummaryEdit, Variant.From<CodeEdit>(ref readOnlySummaryEdit));
		info.AddProperty(PropertyName.previewStatusLabel, Variant.From<Label>(ref previewStatusLabel));
		info.AddProperty(PropertyName.previewGraph, Variant.From<NIntentGraph>(ref previewGraph));
		info.AddProperty(PropertyName.unsavedChangesDialog, Variant.From<ConfirmationDialog>(ref unsavedChangesDialog));
		info.AddProperty(PropertyName.messageDialog, Variant.From<AcceptDialog>(ref messageDialog));
		info.AddProperty(PropertyName.windowPanel, Variant.From<PanelContainer>(ref windowPanel));
		info.AddProperty(PropertyName.editorTabs, Variant.From<TabContainer>(ref editorTabs));
		info.AddProperty(PropertyName.monsterDisplayName, Variant.From<string>(ref monsterDisplayName));
		info.AddProperty(PropertyName.monsterModelFullName, Variant.From<string>(ref monsterModelFullName));
		info.AddProperty(PropertyName.loadedIntentStringsLanguage, Variant.From<string>(ref loadedIntentStringsLanguage));
		info.AddProperty(PropertyName.selectedVariantIndex, Variant.From<int>(ref selectedVariantIndex));
		info.AddProperty(PropertyName.isReady, Variant.From<bool>(ref isReady));
		info.AddProperty(PropertyName.isRefreshingUi, Variant.From<bool>(ref isRefreshingUi));
		info.AddProperty(PropertyName.hasUnsavedChanges, Variant.From<bool>(ref hasUnsavedChanges));
		info.AddProperty(PropertyName.pendingAction, Variant.From<PendingAction>(ref pendingAction));
		info.AddProperty(PropertyName.lastActiveTextInput, Variant.From<Control>(ref lastActiveTextInput));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.monsterNameLabel, ref val))
		{
			monsterNameLabel = ((Variant)(ref val)).As<Label>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName.monsterModelLabel, ref val2))
		{
			monsterModelLabel = ((Variant)(ref val2)).As<Label>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName.headerStatusLabel, ref val3))
		{
			headerStatusLabel = ((Variant)(ref val3)).As<Label>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName.variantList, ref val4))
		{
			variantList = ((Variant)(ref val4)).As<ItemList>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName.stateIdList, ref val5))
		{
			stateIdList = ((Variant)(ref val5)).As<ItemList>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName.addVariantButton, ref val6))
		{
			addVariantButton = ((Variant)(ref val6)).As<Button>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName.duplicateVariantButton, ref val7))
		{
			duplicateVariantButton = ((Variant)(ref val7)).As<Button>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName.deleteVariantButton, ref val8))
		{
			deleteVariantButton = ((Variant)(ref val8)).As<Button>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName.moveVariantUpButton, ref val9))
		{
			moveVariantUpButton = ((Variant)(ref val9)).As<Button>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName.moveVariantDownButton, ref val10))
		{
			moveVariantDownButton = ((Variant)(ref val10)).As<Button>();
		}
		Variant val11 = default(Variant);
		if (info.TryGetProperty(PropertyName.saveButton, ref val11))
		{
			saveButton = ((Variant)(ref val11)).As<Button>();
		}
		Variant val12 = default(Variant);
		if (info.TryGetProperty(PropertyName.reloadButton, ref val12))
		{
			reloadButton = ((Variant)(ref val12)).As<Button>();
		}
		Variant val13 = default(Variant);
		if (info.TryGetProperty(PropertyName.closeButton, ref val13))
		{
			closeButton = ((Variant)(ref val13)).As<Button>();
		}
		Variant val14 = default(Variant);
		if (info.TryGetProperty(PropertyName.conditionEdit, ref val14))
		{
			conditionEdit = ((Variant)(ref val14)).As<LineEdit>();
		}
		Variant val15 = default(Variant);
		if (info.TryGetProperty(PropertyName.conditionStatusLabel, ref val15))
		{
			conditionStatusLabel = ((Variant)(ref val15)).As<Label>();
		}
		Variant val16 = default(Variant);
		if (info.TryGetProperty(PropertyName.upToDateConditionEdit, ref val16))
		{
			upToDateConditionEdit = ((Variant)(ref val16)).As<LineEdit>();
		}
		Variant val17 = default(Variant);
		if (info.TryGetProperty(PropertyName.upToDateConditionStatusLabel, ref val17))
		{
			upToDateConditionStatusLabel = ((Variant)(ref val17)).As<Label>();
		}
		Variant val18 = default(Variant);
		if (info.TryGetProperty(PropertyName.offsetXEdit, ref val18))
		{
			offsetXEdit = ((Variant)(ref val18)).As<LineEdit>();
		}
		Variant val19 = default(Variant);
		if (info.TryGetProperty(PropertyName.offsetYEdit, ref val19))
		{
			offsetYEdit = ((Variant)(ref val19)).As<LineEdit>();
		}
		Variant val20 = default(Variant);
		if (info.TryGetProperty(PropertyName.secondaryInitialStatesEdit, ref val20))
		{
			secondaryInitialStatesEdit = ((Variant)(ref val20)).As<CodeEdit>();
		}
		Variant val21 = default(Variant);
		if (info.TryGetProperty(PropertyName.stateMachineEdit, ref val21))
		{
			stateMachineEdit = ((Variant)(ref val21)).As<CodeEdit>();
		}
		Variant val22 = default(Variant);
		if (info.TryGetProperty(PropertyName.moveReplacementsEdit, ref val22))
		{
			moveReplacementsEdit = ((Variant)(ref val22)).As<CodeEdit>();
		}
		Variant val23 = default(Variant);
		if (info.TryGetProperty(PropertyName.graphPatchEdit, ref val23))
		{
			graphPatchEdit = ((Variant)(ref val23)).As<CodeEdit>();
		}
		Variant val24 = default(Variant);
		if (info.TryGetProperty(PropertyName.intentStringsStatusLabel, ref val24))
		{
			intentStringsStatusLabel = ((Variant)(ref val24)).As<Label>();
		}
		Variant val25 = default(Variant);
		if (info.TryGetProperty(PropertyName.intentStringsEdit, ref val25))
		{
			intentStringsEdit = ((Variant)(ref val25)).As<CodeEdit>();
		}
		Variant val26 = default(Variant);
		if (info.TryGetProperty(PropertyName.readOnlySummaryEdit, ref val26))
		{
			readOnlySummaryEdit = ((Variant)(ref val26)).As<CodeEdit>();
		}
		Variant val27 = default(Variant);
		if (info.TryGetProperty(PropertyName.previewStatusLabel, ref val27))
		{
			previewStatusLabel = ((Variant)(ref val27)).As<Label>();
		}
		Variant val28 = default(Variant);
		if (info.TryGetProperty(PropertyName.previewGraph, ref val28))
		{
			previewGraph = ((Variant)(ref val28)).As<NIntentGraph>();
		}
		Variant val29 = default(Variant);
		if (info.TryGetProperty(PropertyName.unsavedChangesDialog, ref val29))
		{
			unsavedChangesDialog = ((Variant)(ref val29)).As<ConfirmationDialog>();
		}
		Variant val30 = default(Variant);
		if (info.TryGetProperty(PropertyName.messageDialog, ref val30))
		{
			messageDialog = ((Variant)(ref val30)).As<AcceptDialog>();
		}
		Variant val31 = default(Variant);
		if (info.TryGetProperty(PropertyName.windowPanel, ref val31))
		{
			windowPanel = ((Variant)(ref val31)).As<PanelContainer>();
		}
		Variant val32 = default(Variant);
		if (info.TryGetProperty(PropertyName.editorTabs, ref val32))
		{
			editorTabs = ((Variant)(ref val32)).As<TabContainer>();
		}
		Variant val33 = default(Variant);
		if (info.TryGetProperty(PropertyName.monsterDisplayName, ref val33))
		{
			monsterDisplayName = ((Variant)(ref val33)).As<string>();
		}
		Variant val34 = default(Variant);
		if (info.TryGetProperty(PropertyName.monsterModelFullName, ref val34))
		{
			monsterModelFullName = ((Variant)(ref val34)).As<string>();
		}
		Variant val35 = default(Variant);
		if (info.TryGetProperty(PropertyName.loadedIntentStringsLanguage, ref val35))
		{
			loadedIntentStringsLanguage = ((Variant)(ref val35)).As<string>();
		}
		Variant val36 = default(Variant);
		if (info.TryGetProperty(PropertyName.selectedVariantIndex, ref val36))
		{
			selectedVariantIndex = ((Variant)(ref val36)).As<int>();
		}
		Variant val37 = default(Variant);
		if (info.TryGetProperty(PropertyName.isReady, ref val37))
		{
			isReady = ((Variant)(ref val37)).As<bool>();
		}
		Variant val38 = default(Variant);
		if (info.TryGetProperty(PropertyName.isRefreshingUi, ref val38))
		{
			isRefreshingUi = ((Variant)(ref val38)).As<bool>();
		}
		Variant val39 = default(Variant);
		if (info.TryGetProperty(PropertyName.hasUnsavedChanges, ref val39))
		{
			hasUnsavedChanges = ((Variant)(ref val39)).As<bool>();
		}
		Variant val40 = default(Variant);
		if (info.TryGetProperty(PropertyName.pendingAction, ref val40))
		{
			pendingAction = ((Variant)(ref val40)).As<PendingAction>();
		}
		Variant val41 = default(Variant);
		if (info.TryGetProperty(PropertyName.lastActiveTextInput, ref val41))
		{
			lastActiveTextInput = ((Variant)(ref val41)).As<Control>();
		}
	}
}
