using Graybox.DataStructures.Geometric;
using Graybox.DataStructures.MapObjects;
using Graybox.Editor.Documents;
using System.Collections.Generic;
using System.Linq;

namespace Graybox.Editor.Actions.MapObjects.Operations
{
	/// <summary>
	/// Perform: Clips the given objects by the given plane, reselecting the new solids if needed.
	/// Reverse: Removes the clipped objects and restores the originals, reselecting if needed.
	/// </summary>
	public class Clip : CreateEditDelete
	{
		private Plane _plane;
		private List<Solid> _objects;
		private bool _firstRun;
		private bool _keepFront;
		private bool _keepBack;

		public Clip( IEnumerable<Solid> objects, Plane plane, bool keepFront, bool keepBack )
		{
			_objects = objects.Where( x => x.IsValid() ).ToList();
			_plane = plane;
			_keepFront = keepFront;
			_keepBack = keepBack;
			_firstRun = true;
		}

		public override void Dispose()
		{
			_objects = null;
			base.Dispose();
		}

		public override void Perform( Document document )
		{
			if ( _firstRun )
			{
				_firstRun = false;
				foreach ( Solid solid in _objects )
				{
					solid.Split( _plane, document.Map.IDGenerator, out var front, out var back );

					if ( back != null && _keepBack )
					{
						Create( solid.Parent.ID, back );
						back.IsSelected = solid.IsSelected;
					}

					if ( front != null && _keepFront )
					{
						Create( solid.Parent.ID, front );
						front.IsSelected = solid.IsSelected;
					}

					Delete( solid.ID );
				}
			}
			base.Perform( document );
		}
	}
}
