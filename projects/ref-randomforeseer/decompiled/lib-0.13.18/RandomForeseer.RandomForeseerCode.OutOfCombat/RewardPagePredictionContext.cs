using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class RewardPagePredictionContext
{
	[CompilerGenerated]
	private sealed class _003CFlattenRewards_003Ed__3 : IEnumerable<Reward>, IEnumerable, IEnumerator<Reward>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private Reward _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private IEnumerable<Reward> rewards;

		public IEnumerable<Reward> _003C_003E3__rewards;

		private IEnumerator<Reward> _003C_003E7__wrap1;

		private Reward _003Creward_003E5__3;

		private IEnumerator<Reward> _003C_003E7__wrap3;

		Reward IEnumerator<Reward>.Current
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
		public _003CFlattenRewards_003Ed__3(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			int num = _003C_003E1__state;
			if ((uint)(num - -4) <= 1u || (uint)(num - 1) <= 1u)
			{
				try
				{
					if (num == -4 || num == 2)
					{
						try
						{
						}
						finally
						{
							_003C_003Em__Finally2();
						}
					}
				}
				finally
				{
					_003C_003Em__Finally1();
				}
			}
			_003C_003E7__wrap1 = null;
			_003Creward_003E5__3 = null;
			_003C_003E7__wrap3 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			try
			{
				switch (_003C_003E1__state)
				{
				default:
					return false;
				case 0:
					_003C_003E1__state = -1;
					_003C_003E7__wrap1 = rewards.GetEnumerator();
					_003C_003E1__state = -3;
					break;
				case 1:
				{
					_003C_003E1__state = -3;
					Reward obj = _003Creward_003E5__3;
					LinkedRewardSet val = (LinkedRewardSet)(object)((obj is LinkedRewardSet) ? obj : null);
					if (val == null)
					{
						break;
					}
					_003C_003E7__wrap3 = FlattenRewards(val.Rewards).GetEnumerator();
					_003C_003E1__state = -4;
					goto IL_00cd;
				}
				case 2:
					{
						_003C_003E1__state = -4;
						goto IL_00cd;
					}
					IL_00cd:
					if (_003C_003E7__wrap3.MoveNext())
					{
						Reward current = _003C_003E7__wrap3.Current;
						_003C_003E2__current = current;
						_003C_003E1__state = 2;
						return true;
					}
					_003C_003Em__Finally2();
					_003C_003E7__wrap3 = null;
					_003Creward_003E5__3 = null;
					break;
				}
				if (_003C_003E7__wrap1.MoveNext())
				{
					_003Creward_003E5__3 = _003C_003E7__wrap1.Current;
					_003C_003E2__current = _003Creward_003E5__3;
					_003C_003E1__state = 1;
					return true;
				}
				_003C_003Em__Finally1();
				_003C_003E7__wrap1 = null;
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
			if (_003C_003E7__wrap1 != null)
			{
				_003C_003E7__wrap1.Dispose();
			}
		}

		private void _003C_003Em__Finally2()
		{
			_003C_003E1__state = -3;
			if (_003C_003E7__wrap3 != null)
			{
				_003C_003E7__wrap3.Dispose();
			}
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<Reward> IEnumerable<Reward>.GetEnumerator()
		{
			_003CFlattenRewards_003Ed__3 _003CFlattenRewards_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CFlattenRewards_003Ed__ = this;
			}
			else
			{
				_003CFlattenRewards_003Ed__ = new _003CFlattenRewards_003Ed__3(0);
			}
			_003CFlattenRewards_003Ed__.rewards = _003C_003E3__rewards;
			return _003CFlattenRewards_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<Reward>)this).GetEnumerator();
		}
	}

	private static readonly ConditionalWeakTable<RelicModel, RewardsSet> RewardsSets = new ConditionalWeakTable<RelicModel, RewardsSet>();

	public static void Register(RewardsSet rewardsSet)
	{
		foreach (RelicReward item in FlattenRewards(rewardsSet.Rewards).OfType<RelicReward>())
		{
			RelicModel relic = item.Relic;
			if (relic != null)
			{
				RewardsSets.AddOrUpdate(relic, rewardsSet);
			}
		}
	}

	public static bool HasOtherPendingReward(RelicModel relic)
	{
		RelicModel relic2 = relic;
		if (!RewardsSets.TryGetValue(relic2, out RewardsSet value))
		{
			return false;
		}
		return FlattenRewards(value.Rewards).Any(delegate(Reward other)
		{
			if (!other.SuccessfullySelected)
			{
				RelicReward val = (RelicReward)(object)((other is RelicReward) ? other : null);
				if (val != null)
				{
					return val.Relic != relic2;
				}
				return true;
			}
			return false;
		});
	}

	[IteratorStateMachine(typeof(_003CFlattenRewards_003Ed__3))]
	private static IEnumerable<Reward> FlattenRewards(IEnumerable<Reward> rewards)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CFlattenRewards_003Ed__3(-2)
		{
			_003C_003E3__rewards = rewards
		};
	}
}
