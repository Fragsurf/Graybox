
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Graybox;

public class Profiler
{
	private ConcurrentBag<SampleData> samples = new ConcurrentBag<SampleData>();
	private Stopwatch stopwatch = new Stopwatch();
	private object lockObject = new object();

	public Profiler()
	{
		stopwatch.Start();
	}

	public Sample Begin( string sampleName )
	{
		return new Sample( sampleName, this );
	}

	private void AddSample( SampleData data )
	{
		samples.Add( data );
	}

	public class Sample : IDisposable
	{
		private string sampleName;
		private long startTime;
		private long startMemory;
		private Profiler profile;

		public Sample( string name, Profiler profile )
		{
			this.profile = profile;
			sampleName = name;
			startTime = profile.stopwatch.ElapsedMilliseconds;
			startMemory = GC.GetTotalMemory( false );
		}

		public void Dispose()
		{
			long endTime = profile.stopwatch.ElapsedMilliseconds;
			long endMemory = GC.GetTotalMemory( false );
			profile.AddSample( new SampleData
			{
				Name = sampleName,
				StartTime = startTime,
				EndTime = endTime,
				StartMemory = startMemory,
				EndMemory = endMemory,
				Duration = endTime - startTime,
				MemoryUsed = endMemory - startMemory
			} );
		}
	}

	public struct SampleData
	{
		public string Name;
		public long StartTime;
		public long EndTime;
		public long StartMemory;
		public long EndMemory;
		public long Duration;
		public long MemoryUsed;
	}

	public Dictionary<string, SampleData> GetMergedSamples()
	{
		lock ( lockObject )
		{
			var groupedSamples = samples.GroupBy( s => s.Name ).Select( group => new SampleData
			{
				Name = group.Key,
				Duration = group.Sum( g => g.Duration ),
				MemoryUsed = group.Sum( g => g.MemoryUsed )
			} ).OrderByDescending( s => s.Duration ).ToDictionary( sample => sample.Name );

			return groupedSamples;
		}
	}
}
