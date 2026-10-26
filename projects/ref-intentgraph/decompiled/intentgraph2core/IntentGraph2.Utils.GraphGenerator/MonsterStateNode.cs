using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using IntentGraph2.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace IntentGraph2.Utils.GraphGenerator;

internal class MonsterStateNode
{
	[CompilerGenerated]
	private sealed class _003CGetAllDescendants_003Ed__114 : IEnumerable<MonsterStateNode>, IEnumerable, IEnumerator<MonsterStateNode>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private MonsterStateNode _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		public MonsterStateNode _003C_003E4__this;

		private List<MonsterStateNode>.Enumerator _003C_003E7__wrap1;

		private MonsterStateNode _003Cchild_003E5__3;

		private IEnumerator<MonsterStateNode> _003C_003E7__wrap3;

		MonsterStateNode IEnumerator<MonsterStateNode>.Current
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
		public _003CGetAllDescendants_003Ed__114(int _003C_003E1__state)
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
			_003C_003E7__wrap1 = default(List<MonsterStateNode>.Enumerator);
			_003Cchild_003E5__3 = null;
			_003C_003E7__wrap3 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			try
			{
				int num = _003C_003E1__state;
				MonsterStateNode monsterStateNode = _003C_003E4__this;
				switch (num)
				{
				default:
					return false;
				case 0:
					_003C_003E1__state = -1;
					if (monsterStateNode.Children == null)
					{
						break;
					}
					_003C_003E7__wrap1 = monsterStateNode.Children.GetEnumerator();
					_003C_003E1__state = -3;
					goto IL_00f1;
				case 1:
					_003C_003E1__state = -3;
					_003C_003E7__wrap3 = _003Cchild_003E5__3.GetAllDescendants().GetEnumerator();
					_003C_003E1__state = -4;
					goto IL_00d0;
				case 2:
					{
						_003C_003E1__state = -4;
						goto IL_00d0;
					}
					IL_00d0:
					if (_003C_003E7__wrap3.MoveNext())
					{
						MonsterStateNode current = _003C_003E7__wrap3.Current;
						_003C_003E2__current = current;
						_003C_003E1__state = 2;
						return true;
					}
					_003C_003Em__Finally2();
					_003C_003E7__wrap3 = null;
					_003Cchild_003E5__3 = null;
					goto IL_00f1;
					IL_00f1:
					if (_003C_003E7__wrap1.MoveNext())
					{
						_003Cchild_003E5__3 = _003C_003E7__wrap1.Current;
						_003C_003E2__current = _003Cchild_003E5__3;
						_003C_003E1__state = 1;
						return true;
					}
					_003C_003Em__Finally1();
					_003C_003E7__wrap1 = default(List<MonsterStateNode>.Enumerator);
					break;
				}
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
			((IDisposable)_003C_003E7__wrap1).Dispose();
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
		IEnumerator<MonsterStateNode> IEnumerable<MonsterStateNode>.GetEnumerator()
		{
			_003CGetAllDescendants_003Ed__114 result;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				result = this;
			}
			else
			{
				result = new _003CGetAllDescendants_003Ed__114(0)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			return result;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<MonsterStateNode>)this).GetEnumerator();
		}
	}

	public string? Id { get; set; }

	public float Width { get; set; }

	public float Height { get; set; }

	public MonsterStateNode? Parent { get; set; }

	public List<MonsterStateNode>? Children { get; set; }

	public bool HorizontalLayout { get; set; }

	public MonsterStateNodeLabel? Label { get; set; }

	public MonsterState? State { get; set; }

	public List<ResolvedIntent>? ResolvedIntentIcons { get; set; }

	public MonsterStateNode? NextState { get; set; }

	public List<string> MoveStateIds { get; set; } = new List<string>();


	public bool IsInitialState { get; set; }

	public int NextStateCount { get; set; }

	public bool UnrecognizedStateType { get; set; }

	public bool ForceNotSimpleLoop { get; set; }

	public bool SimpleLoopStart { get; set; }

	public int SimpleLoopLength { get; set; }

	public int SimpleLoopPrecessorCount { get; set; }

	public Position Offset { get; set; }

	public float X { get; set; }

	public float Y { get; set; }

	public bool AddedToGraph { get; set; }

	public int XIndex { get; set; }

	public int YIndex { get; set; }

	public bool AddedArrow { get; set; }

	public float ArrowRight { get; set; }

	public float ArrowBottom { get; set; }

	public string? FullId
	{
		get
		{
			if (Parent == null)
			{
				if (Id != null)
				{
					return "/" + Id;
				}
				return null;
			}
			string fullId = Parent.FullId;
			if (fullId != null && Id != null)
			{
				return fullId + "/" + Id;
			}
			return null;
		}
	}

	public void CalculateNodeSize()
	{
		List<MonsterStateNode> children = Children;
		if (children == null)
		{
			return;
		}
		if (HorizontalLayout)
		{
			Width = children.Select((MonsterStateNode c) => c.Width).Sum() + 0.1f * (float)(children.Count - 1) + 0.2f;
			Height = children.Select((MonsterStateNode c) => c.Height).DefaultIfEmpty(1f).Max() + 0.25f + 0.2f + (children.All((MonsterStateNode c) => c.Children == null) ? IntentGraphGenerator.IconGroupSingleMovePadding : 0f);
		}
		else
		{
			Width = children.Select((MonsterStateNode c) => c.Width).DefaultIfEmpty(1f).Max() + 0.2f;
			Height = 0.25f * (float)children.Count + 0.2f + children.Select((MonsterStateNode c) => c.Height).Sum() + IntentGraphGenerator.IconGroupSingleMovePadding * (float)children.Where((MonsterStateNode c) => c.Children == null).Count();
		}
	}

	public HashSet<MonsterStateNode> GetAllNodes()
	{
		HashSet<MonsterStateNode> hashSet = new HashSet<MonsterStateNode>();
		Queue<MonsterStateNode> queue = new Queue<MonsterStateNode>();
		queue.Enqueue(this);
		while (queue.Count > 0)
		{
			MonsterStateNode monsterStateNode = queue.Dequeue();
			if (!hashSet.Add(monsterStateNode))
			{
				continue;
			}
			if (monsterStateNode.Children != null)
			{
				foreach (MonsterStateNode child in monsterStateNode.Children)
				{
					queue.Enqueue(child);
				}
			}
			if (monsterStateNode.NextState != null)
			{
				queue.Enqueue(monsterStateNode.NextState);
			}
		}
		return hashSet;
	}

	public void AddMoveStateIdsFrom(MonsterStateNode b)
	{
		MoveStateIds.AddRange(b.MoveStateIds);
		if (Children != null && b.Children != null)
		{
			for (int i = 0; i < Children.Count; i++)
			{
				Children[i].AddMoveStateIdsFrom(b.Children[i]);
			}
		}
	}

	public void SetIsInitialState(bool value)
	{
		IsInitialState = value;
		if (Children == null)
		{
			return;
		}
		foreach (MonsterStateNode child in Children)
		{
			child.SetIsInitialState(value);
		}
	}

	[IteratorStateMachine(typeof(_003CGetAllDescendants_003Ed__114))]
	public IEnumerable<MonsterStateNode> GetAllDescendants()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetAllDescendants_003Ed__114(-2)
		{
			_003C_003E4__this = this
		};
	}
}
