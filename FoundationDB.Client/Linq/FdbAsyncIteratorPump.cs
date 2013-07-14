﻿#region BSD Licence
/* Copyright (c) 2013, Doxense SARL
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
	* Redistributions of source code must retain the above copyright
	  notice, this list of conditions and the following disclaimer.
	* Redistributions in binary form must reproduce the above copyright
	  notice, this list of conditions and the following disclaimer in the
	  documentation and/or other materials provided with the distribution.
	* Neither the name of Doxense nor the
	  names of its contributors may be used to endorse or promote products
	  derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#endregion

namespace FoundationDB.Linq
{
	using FoundationDB.Async;
	using FoundationDB.Client.Utils;
	using System;
	using System.Diagnostics;
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>Pump that repeatedly calls MoveNext on an iterator and tries to publish the values in a Producer/Consumer queue</summary>
	/// <typeparam name="TInput"></typeparam>
	[DebuggerDisplay("State={m_state}")]
	internal sealed class FdbAsyncIteratorPump<TInput>
	{
		private const int STATE_IDLE = 0;
		private const int STATE_WAITING_FOR_NEXT = 1;
		private const int STATE_PUBLISHING_TO_TARGET = 2;
		private const int STATE_FAILED = 3;
		private const int STATE_DONE = 4;

		private volatile int m_state;
		private readonly IFdbAsyncEnumerator<TInput> m_iterator;
		private readonly IFdbAsyncTarget<TInput> m_target;

		public FdbAsyncIteratorPump(
			IFdbAsyncEnumerator<TInput> iterator,
			IFdbAsyncTarget<TInput> target
		)
		{
			Contract.Requires(iterator != null);
			Contract.Requires(target != null);

			m_iterator = iterator;
			m_target = target;
		}

		/// <summary>Returns true if the pump has completed (with success or failure)</summary>
		public bool IsCompleted
		{
			get { return m_state >= STATE_FAILED; }
		}

		internal int State
		{
			get { return m_state; }
		}

		/// <summary>Run the pump until the inner iterator is done, an error occurs, or the cancellation token is fired</summary>
		public async Task PumpAsync(CancellationToken ct)
		{
			if (m_state != STATE_IDLE)
			{
				if (m_state >= STATE_FAILED)
					throw new InvalidOperationException("The iterator pump has already completed once");
				else
					throw new InvalidOperationException("The iterator pump is already running");
			}

			try
			{			
				while (!ct.IsCancellationRequested)
				{
					m_state = STATE_WAITING_FOR_NEXT;
					if (!(await m_iterator.MoveNext(ct).ConfigureAwait(false)))
					{
						m_state = STATE_DONE;
						m_target.OnCompleted();
						return;
					}

					m_state = STATE_PUBLISHING_TO_TARGET;
					await m_target.OnNextAsync(m_iterator.Current, ct).ConfigureAwait(false);
				}

				// push the cancellation on the queue
				OnError(new OperationCanceledException(ct));
				// and throw!
			}
			catch (Exception e)
			{
				if (m_state == STATE_FAILED)
				{ // already signaled the target, just throw
					throw;
				}

				// push the error on the queue, and eat the error
				OnError(e);
			}
			finally
			{
				if (m_state != STATE_FAILED) m_state = STATE_DONE;
			}
		}

		private void OnError(Exception e)
		{
			try
			{
				m_state = STATE_FAILED;
				m_target.OnError(e);
			}
			catch
			{
				//TODO ?
			}
		}

	}

}
