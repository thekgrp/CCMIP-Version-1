using System;
using System.Collections.Generic;

namespace CoreCommandMIP.Client
{
	internal sealed class TrackChangeInfo
	{
		internal long? Counter { get; set; }
		internal List<long> TrackIds { get; } = new List<long>();
	}

	internal sealed class TrackFetchResult
	{
		internal TrackFetchResult(bool hasChanges, long? changeCounter, IReadOnlyList<SmartMapLocation> tracks)
		{
			HasChanges = hasChanges;
			ChangeCounter = changeCounter;
			Tracks = tracks ?? Array.Empty<SmartMapLocation>();
		}

		internal bool HasChanges { get; }
		internal long? ChangeCounter { get; }
		internal IReadOnlyList<SmartMapLocation> Tracks { get; }

		internal static TrackFetchResult NoChanges(long? counter)
		{
			return new TrackFetchResult(false, counter, Array.Empty<SmartMapLocation>());
		}
	}
}
