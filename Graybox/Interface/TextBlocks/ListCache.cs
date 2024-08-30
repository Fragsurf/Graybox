﻿
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Graybox.Interface.TextBlocks
{

	internal class ListPool<T>
	{


		private readonly ConcurrentBag<List<T>> Pool = new ConcurrentBag<List<T>>();

		public List<T> Get() => Pool.TryTake( out var result ) ? result : new List<T>();

		public void Return( List<T> list )
		{
			list.Clear();
			Pool.Add( list );
		}
	}

}
