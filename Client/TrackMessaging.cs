using System;
using System.Collections.Generic;

namespace CoreCommandMIP.Client
{
    internal sealed class TrackListMessage
    {
        internal TrackListMessage(Guid configurationId, IReadOnlyList<SmartMapLocation> tracks)
        {
            ConfigurationId = configurationId;
            Tracks = tracks ?? Array.Empty<SmartMapLocation>();
        }

        internal Guid ConfigurationId { get; }
        internal IReadOnlyList<SmartMapLocation> Tracks { get; }
    }

    internal sealed class TrackSelectionMessage
    {
        internal TrackSelectionMessage(Guid configurationId, SmartMapLocation track)
        {
            ConfigurationId = configurationId;
            Track = track;
        }

        internal Guid ConfigurationId { get; }
        internal SmartMapLocation Track { get; }
    }
}
