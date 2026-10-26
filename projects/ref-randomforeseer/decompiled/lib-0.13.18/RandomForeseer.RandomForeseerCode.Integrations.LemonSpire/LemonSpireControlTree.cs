using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;

namespace RandomForeseer.RandomForeseerCode.Integrations.LemonSpire;

internal static class LemonSpireControlTree
{
	[CompilerGenerated]
	private sealed class _003CDescendants_003Ed__2 : IEnumerable<Control>, IEnumerable, IEnumerator<Control>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private Control _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private Control parent;

		public Control _003C_003E3__parent;

		private IEnumerator<Control> _003C_003E7__wrap1;

		private Control _003Cchild_003E5__3;

		private IEnumerator<Control> _003C_003E7__wrap3;

		Control IEnumerator<Control>.Current
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
		public _003CDescendants_003Ed__2(int _003C_003E1__state)
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
			_003Cchild_003E5__3 = null;
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
					_003C_003E7__wrap1 = ((IEnumerable)((Node)parent).GetChildren(false)).OfType<Control>().GetEnumerator();
					_003C_003E1__state = -3;
					break;
				case 1:
					_003C_003E1__state = -3;
					_003C_003E7__wrap3 = Descendants(_003Cchild_003E5__3).GetEnumerator();
					_003C_003E1__state = -4;
					goto IL_00c9;
				case 2:
					{
						_003C_003E1__state = -4;
						goto IL_00c9;
					}
					IL_00c9:
					if (_003C_003E7__wrap3.MoveNext())
					{
						Control current = _003C_003E7__wrap3.Current;
						_003C_003E2__current = current;
						_003C_003E1__state = 2;
						return true;
					}
					_003C_003Em__Finally2();
					_003C_003E7__wrap3 = null;
					_003Cchild_003E5__3 = null;
					break;
				}
				if (_003C_003E7__wrap1.MoveNext())
				{
					_003Cchild_003E5__3 = _003C_003E7__wrap1.Current;
					_003C_003E2__current = _003Cchild_003E5__3;
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
		IEnumerator<Control> IEnumerable<Control>.GetEnumerator()
		{
			_003CDescendants_003Ed__2 _003CDescendants_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CDescendants_003Ed__ = this;
			}
			else
			{
				_003CDescendants_003Ed__ = new _003CDescendants_003Ed__2(0);
			}
			_003CDescendants_003Ed__.parent = _003C_003E3__parent;
			return _003CDescendants_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<Control>)this).GetEnumerator();
		}
	}

	public static HashSet<Control> SnapshotDescendants(Control parent)
	{
		return Descendants(parent).ToHashSet();
	}

	public static IEnumerable<Control> NewDescendants(Control parent, HashSet<Control> before)
	{
		HashSet<Control> before2 = before;
		return from control in Descendants(parent)
			where !before2.Contains(control)
			select control;
	}

	[IteratorStateMachine(typeof(_003CDescendants_003Ed__2))]
	public static IEnumerable<Control> Descendants(Control parent)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CDescendants_003Ed__2(-2)
		{
			_003C_003E3__parent = parent
		};
	}
}
