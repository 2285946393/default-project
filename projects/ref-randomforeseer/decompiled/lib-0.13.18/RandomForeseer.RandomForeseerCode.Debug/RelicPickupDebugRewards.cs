using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace RandomForeseer.RandomForeseerCode.Debug;

internal static class RelicPickupDebugRewards
{
	[CompilerGenerated]
	private sealed class _003CCreateRelics_003Ed__7 : IEnumerable<RelicModel>, IEnumerable, IEnumerator<RelicModel>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private RelicModel _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		RelicModel IEnumerator<RelicModel>.Current
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
		public _003CCreateRelics_003Ed__7(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			switch (_003C_003E1__state)
			{
			default:
				return false;
			case 0:
				_003C_003E1__state = -1;
				_003C_003E2__current = (RelicModel)(object)ModelDb.Relic<Cauldron>();
				_003C_003E1__state = 1;
				return true;
			case 1:
				_003C_003E1__state = -1;
				_003C_003E2__current = (RelicModel)(object)ModelDb.Relic<FragrantMushroom>();
				_003C_003E1__state = 2;
				return true;
			case 2:
				_003C_003E1__state = -1;
				_003C_003E2__current = (RelicModel)(object)ModelDb.Relic<Orrery>();
				_003C_003E1__state = 3;
				return true;
			case 3:
				_003C_003E1__state = -1;
				_003C_003E2__current = (RelicModel)(object)ModelDb.Relic<WarPaint>();
				_003C_003E1__state = 4;
				return true;
			case 4:
				_003C_003E1__state = -1;
				_003C_003E2__current = (RelicModel)(object)ModelDb.Relic<Whetstone>();
				_003C_003E1__state = 5;
				return true;
			case 5:
				_003C_003E1__state = -1;
				return false;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<RelicModel> IEnumerable<RelicModel>.GetEnumerator()
		{
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				return this;
			}
			return new _003CCreateRelics_003Ed__7(0);
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<RelicModel>)this).GetEnumerator();
		}
	}

	[CompilerGenerated]
	private sealed class _003CCreateTreasureTestRelics_003Ed__8 : IEnumerable<RelicModel>, IEnumerable, IEnumerator<RelicModel>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private RelicModel _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		RelicModel IEnumerator<RelicModel>.Current
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
		public _003CCreateTreasureTestRelics_003Ed__8(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			switch (_003C_003E1__state)
			{
			default:
				return false;
			case 0:
				_003C_003E1__state = -1;
				_003C_003E2__current = (RelicModel)(object)ModelDb.Relic<WarPaint>();
				_003C_003E1__state = 1;
				return true;
			case 1:
				_003C_003E1__state = -1;
				_003C_003E2__current = (RelicModel)(object)ModelDb.Relic<Whetstone>();
				_003C_003E1__state = 2;
				return true;
			case 2:
				_003C_003E1__state = -1;
				return false;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<RelicModel> IEnumerable<RelicModel>.GetEnumerator()
		{
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				return this;
			}
			return new _003CCreateTreasureTestRelics_003Ed__8(0);
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<RelicModel>)this).GetEnumerator();
		}
	}

	private static readonly RelicRarity[] TreasureTestRarities;

	public static void OfferPredictedNonAncientRelics()
	{
		TaskHelper.RunSafely(OfferPredictedNonAncientRelicsAsync());
	}

	public static void OpenPredictedTreasureRoom()
	{
		TaskHelper.RunSafely(OpenPredictedTreasureRoomAsync());
	}

	public static void OpenRelicTraderPickupTest()
	{
		TaskHelper.RunSafely(OpenRelicTraderPickupTestAsync());
	}

	private static async Task OfferPredictedNonAncientRelicsAsync()
	{
		RunState val = RunManager.Instance.DebugOnlyGetState();
		if (val == null)
		{
			Entry.Logger.Warn("Cannot offer predicted non-Ancient relic debug rewards: no active run.", 1);
			return;
		}
		Player player = LocalContext.GetMe((IPlayerCollection)(object)val);
		if (player == null)
		{
			Entry.Logger.Warn("Cannot offer predicted non-Ancient relic debug rewards: no active local player.", 1);
			return;
		}
		List<Reward> list = CreateRelics().Select((Func<RelicModel, RelicReward>)((RelicModel relic) => new RelicReward(relic.ToMutable(), player))).Cast<Reward>().ToList();
		await RewardsCmd.OfferCustom(player, list);
	}

	private static async Task OpenPredictedTreasureRoomAsync()
	{
		RunState state = RunManager.Instance.DebugOnlyGetState();
		if (state == null)
		{
			Entry.Logger.Warn("Cannot open predicted treasure room debug test: no active run.", 1);
			return;
		}
		if (LocalContext.GetMe((IPlayerCollection)(object)state) == null)
		{
			Entry.Logger.Warn("Cannot open predicted treasure room debug test: no active local player.", 1);
			return;
		}
		RelicModel val = Rng.Chaotic.NextItem<RelicModel>((IEnumerable<RelicModel>)(from relic in CreateTreasureTestRelics()
			where relic.IsAllowed((IRunState)(object)state)
			select relic).ToList());
		if (val == null)
		{
			Entry.Logger.Warn("Cannot open predicted treasure room debug test: no War Paint or Whetstone target is currently allowed.", 1);
			return;
		}
		SerializableRelicGrabBag val2 = new SerializableRelicGrabBag();
		RelicRarity[] treasureTestRarities = TreasureTestRarities;
		foreach (RelicRarity key in treasureTestRarities)
		{
			val2.RelicIdLists[key] = new List<ModelId>(1) { ((AbstractModel)val).Id };
		}
		state.SharedRelicGrabBag.LoadFromSerializable(val2);
		Entry.Logger.Info($"Opening debug treasure room with predicted relic target {((AbstractModel)val).Id}.", 1);
		await RunManager.Instance.EnterRoomDebug((RoomType)4, (MapPointType)3, (AbstractModel)null, false);
	}

	private static async Task OpenRelicTraderPickupTestAsync()
	{
		RunState val = RunManager.Instance.DebugOnlyGetState();
		if (val == null)
		{
			Entry.Logger.Warn("Cannot open Relic Trader pickup debug test: no active run.", 1);
			return;
		}
		Player me = LocalContext.GetMe((IPlayerCollection)(object)val);
		if (me == null)
		{
			Entry.Logger.Warn("Cannot open Relic Trader pickup debug test: no active local player.", 1);
			return;
		}
		Circlet circlet = await RelicCmd.Obtain<Circlet>(me);
		EventRoom val2 = new EventRoom((EventModel)(object)ModelDb.Event<RelicTrader>());
		val2.set_OnStart((Action<EventModel>)delegate(EventModel eventModel)
		{
			RelicTrader val4 = (RelicTrader)(object)((eventModel is RelicTrader) ? eventModel : null);
			if (val4 != null)
			{
				val4._ownedRelics = new _003C_003Ez__ReadOnlyArray<RelicModel>((RelicModel[])(object)new RelicModel[2]
				{
					(RelicModel)circlet,
					(RelicModel)circlet
				});
				val4._newRelics = new _003C_003Ez__ReadOnlyArray<RelicModel>((RelicModel[])(object)new RelicModel[2]
				{
					(RelicModel)ModelDb.Relic<WarPaint>(),
					(RelicModel)ModelDb.Relic<Whetstone>()
				});
			}
		});
		EventRoom val3 = val2;
		Entry.Logger.Info("Opening debug Relic Trader pickup test with Circlet trades for War Paint and Whetstone.", 1);
		await RunManager.Instance.EnterRoom((AbstractRoom)(object)val3);
	}

	[IteratorStateMachine(typeof(_003CCreateRelics_003Ed__7))]
	private static IEnumerable<RelicModel> CreateRelics()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CCreateRelics_003Ed__7(-2);
	}

	[IteratorStateMachine(typeof(_003CCreateTreasureTestRelics_003Ed__8))]
	private static IEnumerable<RelicModel> CreateTreasureTestRelics()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CCreateTreasureTestRelics_003Ed__8(-2);
	}

	static RelicPickupDebugRewards()
	{
		RelicRarity[] array = new RelicRarity[4];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		TreasureTestRarities = (RelicRarity[])(object)array;
	}
}
