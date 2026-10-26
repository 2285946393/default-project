using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.Common;

internal sealed class PredictionTraceFrame
{
	[CompilerGenerated]
	private sealed class _003CAncestors_003Ed__12 : IEnumerable<PredictionTraceFrame>, IEnumerable, IEnumerator<PredictionTraceFrame>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private PredictionTraceFrame _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		public PredictionTraceFrame _003C_003E4__this;

		private PredictionTraceFrame _003Ccurrent_003E5__2;

		PredictionTraceFrame IEnumerator<PredictionTraceFrame>.Current
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
		public _003CAncestors_003Ed__12(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003Ccurrent_003E5__2 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			int num = _003C_003E1__state;
			PredictionTraceFrame predictionTraceFrame = _003C_003E4__this;
			if (num != 0)
			{
				if (num != 1)
				{
					return false;
				}
				_003C_003E1__state = -1;
				_003Ccurrent_003E5__2 = _003Ccurrent_003E5__2.Parent;
				if (_003Ccurrent_003E5__2 == null)
				{
					return false;
				}
			}
			else
			{
				_003C_003E1__state = -1;
				_003Ccurrent_003E5__2 = predictionTraceFrame;
			}
			_003C_003E2__current = _003Ccurrent_003E5__2;
			_003C_003E1__state = 1;
			return true;
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
		IEnumerator<PredictionTraceFrame> IEnumerable<PredictionTraceFrame>.GetEnumerator()
		{
			_003CAncestors_003Ed__12 result;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				result = this;
			}
			else
			{
				result = new _003CAncestors_003Ed__12(0)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			return result;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<PredictionTraceFrame>)this).GetEnumerator();
		}
	}

	public required PredictionTraceFrame? Parent { get; init; }

	public required AbstractModel Source { get; init; }

	public required PredictionInvocation Invocation { get; init; }

	[IteratorStateMachine(typeof(_003CAncestors_003Ed__12))]
	public IEnumerable<PredictionTraceFrame> Ancestors()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CAncestors_003Ed__12(-2)
		{
			_003C_003E4__this = this
		};
	}
}
