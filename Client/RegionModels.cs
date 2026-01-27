using System;
using System.Collections.Generic;

namespace CoreCommandMIP.Client
{
	internal sealed class RegionVertex
	{
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public double Altitude { get; set; }
	}

	internal sealed class RegionAltitudeLimits
	{
		public double Greater { get; set; }
		public double Lesser { get; set; }
	}

	internal sealed class RegionDefinition
	{
		public bool Active { get; set; }
		public bool Exclusion { get; set; }
		public RegionAltitudeLimits AltitudeLimits { get; set; }
		public string Name { get; set; }
		public string Color { get; set; }
		public double Fill { get; set; }
		public List<RegionVertex> Vertices { get; set; }

		public RegionDefinition()
		{
			Vertices = new List<RegionVertex>();
			AltitudeLimits = new RegionAltitudeLimits();
		}
	}

	internal sealed class RegionListItem
	{
		public long Id { get; set; }
		public string Name { get; set; }
		public bool Active { get; set; }
		public string GuidId { get; set; } // For servers that use GUID instead of numeric ID
		public bool Exclusion { get; set; } // Type: true = Exclusion, false = Alarm
		public bool IsSelected { get; set; } // For DataGrid selection
		
		// Display property for Type column
		public string TypeDisplay => Exclusion ? "Exclusion" : "Alarm";
		
		// Override ToString for proper display in CheckedListBox
		public override string ToString()
		{
			var typeLabel = Exclusion ? "Exclusion" : "Alarm";
			return $"{Name} [{typeLabel}]";
		}
	}
}
