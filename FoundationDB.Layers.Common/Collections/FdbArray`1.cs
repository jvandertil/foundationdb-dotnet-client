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

namespace FoundationDB.Layers.Collections
{
	using FoundationDB.Async;
	using FoundationDB.Client;
	using FoundationDB.Layers.Tuples;
	using System;
	using System.Diagnostics;
	using System.Threading;
	using System.Threading.Tasks;

	[DebuggerDisplay("Subspace={Subspace}")]
	public class FdbArray<TValue>
	{
		public FdbArray(FdbSubspace subspace, ISliceSerializer<TValue> serializer)
		{
			if (subspace == null) throw new ArgumentNullException("subspace");

			this.Array = new FdbArray(subspace);
			this.Serializer = serializer;
		}

		/// <summary>Subspace used as a prefix for all items in this array</summary>
		public FdbSubspace Subspace { get { return this.Array.Subspace; } }

		/// <summary>Class that can serialize/deserialize values into/from slices</summary>
		public ISliceSerializer<TValue> Serializer { get; private set; }

		internal FdbArray Array { get; private set; }

		#region Get / Set / Clear

		public Task<TValue> GetAsync(IFdbReadOnlyTransaction trans, int key)
		{
			return this.Array.GetAsync(trans, key).Then((bytes) => this.Serializer.Deserialize(bytes, default(TValue)));
		}

		public Task<TValue> GetAsync(IFdbReadOnlyTransaction trans, long key)
		{
			return this.Array.GetAsync(trans, key).Then((bytes) => this.Serializer.Deserialize(bytes, default(TValue)));
		}

		public void Set(IFdbTransaction trans, int key, TValue value)
		{
			this.Array.Set(trans, key, this.Serializer.Serialize(value));
		}

		public void Set(IFdbTransaction trans, long key, TValue value)
		{
			this.Array.Set(trans, key, this.Serializer.Serialize(value));
		}

		public void Clear(IFdbTransaction trans)
		{
			this.Array.Clear(trans);
		}

		#endregion

	}

}
