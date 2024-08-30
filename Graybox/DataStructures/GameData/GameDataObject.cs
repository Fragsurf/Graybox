﻿using System.Collections.Generic;
using System.Linq;

namespace Graybox.DataStructures.GameData
{
	public class GameDataObject
	{
		public string Name { get; set; }
		public string DisplayName { get; set; }
		public string Description { get; set; }
		public ClassType ClassType { get; set; }
		public List<string> BaseClasses { get; private set; }
		public List<Behaviour> Behaviours { get; private set; }
		public List<Property> Properties { get; private set; }
		public List<IO> InOuts { get; private set; }
		public bool IsCustom { get; private set; }

		public GameDataObject( string name, string description, ClassType classType, bool custom = false )
		{
			Name = name;
			Description = description;
			ClassType = classType;
			BaseClasses = new List<string>();
			Behaviours = new List<Behaviour>();
			Properties = new List<Property>();
			InOuts = new List<IO>();
			IsCustom = custom;
		}

		public void Inherit( IEnumerable<GameDataObject> parents )
		{
			foreach ( GameDataObject gdo in parents )
			{
				MergeBehaviours( gdo.Behaviours );
				MergeProperties( gdo.Properties );
				MergeInOuts( gdo.InOuts );
			}
		}

		private void MergeInOuts( IEnumerable<IO> inOuts )
		{
			int inc = 0;
			foreach ( IO io in inOuts )
			{
				IO existing = InOuts.FirstOrDefault( x => x.IOType == io.IOType && x.Name == io.Name );
				if ( existing == null ) InOuts.Insert( inc++, io );
			}
		}

		private void MergeProperties( IEnumerable<Property> properties )
		{
			int inc = 0;
			foreach ( Property p in properties )
			{
				Property existing = Properties.FirstOrDefault( x => x.Name == p.Name );
				if ( existing != null ) existing.Options.AddRange( p.Options.Where( x => !existing.Options.Contains( x ) ) );
				else Properties.Insert( inc++, p );
			}
		}

		private void MergeBehaviours( IEnumerable<Behaviour> behaviours )
		{
			int inc = 0;
			foreach ( Behaviour b in behaviours )
			{
				Behaviour existing = Behaviours.FirstOrDefault( x => x.Name == b.Name );
				if ( existing != null ) existing.Values.AddRange( b.Values.Where( x => !existing.Values.Contains( x ) ) );
				else Behaviours.Insert( inc++, b );
			}
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
