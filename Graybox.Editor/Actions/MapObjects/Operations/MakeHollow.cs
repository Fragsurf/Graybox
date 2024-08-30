using Graybox.DataStructures.Geometric;
using Graybox.DataStructures.MapObjects;
using Graybox.DataStructures.Transformations;
using Graybox.Editor.Documents;
using System.Collections.Generic;
using System.Linq;

namespace Graybox.Editor.Actions.MapObjects.Operations
{
	/// <summary>
	/// Perform: Makes the given objects hollow, reselecting the new solids if needed.
	/// Reverse: Removes the hollowed objects and restores the originals, reselecting if needed.
	/// Compare to the carve and clip operations, these are enormously similar.
	/// </summary>
	public class MakeHollow : CreateEditDelete
	{
		private readonly float _width;
		private List<Solid> _objects;
		private bool _firstRun;

		public MakeHollow( IEnumerable<Solid> objects, float width )
		{
			_objects = objects.ToList();
			_width = width;
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
				foreach ( Solid obj in _objects )
				{
					bool split = false;
					Solid solid = obj;

					// Make a scaled version of the solid for the "inside" of the hollowed solid
					OpenTK.Mathematics.Vector3 origin = solid.CalculateWorldCenter();
					OpenTK.Mathematics.Vector3 current = obj.BoundingBox.Dimensions;
					OpenTK.Mathematics.Vector3 target = current - (new OpenTK.Mathematics.Vector3( _width, _width, _width ) * 2); // Double the width to take from both sides
																								  // Ensure we don't have any invalid target sizes
					if ( target.X < 1 ) target.X = 1;
					if ( target.Y < 1 ) target.Y = 1;
					if ( target.Z < 1 ) target.Z = 1;

					// Clone and scale the solid
					OpenTK.Mathematics.Vector3 scale = target.ComponentDivide( current );
					UnitScale transform = new UnitScale( scale, origin );
					Solid carver = (Solid)solid.Clone();
					carver.Transform( transform, document.Map.GetTransformFlags() );

					// For a negative width, we want the original solid to be the inside instead
					if ( _width < 0 )
					{
						Solid temp = carver;
						carver = solid;
						solid = temp;
					}

					// Carve the outside solid with the inside solid
					foreach ( Plane plane in carver.Faces.Select( x => x.Plane ) )
					{
						// Split solid by plane
						Solid back, front;
						try
						{
							if ( !solid.Split( plane, out back, out front, document.Map.IDGenerator ) ) continue;
						}
						catch
						{
							// We're not too fussy about over-complicated carving, just get out if we've broken it.
							break;
						}
						split = true;

						if ( front != null )
						{
							// Retain the front solid
							if ( obj.IsSelected ) front.IsSelected = true;
							Create( obj.Parent.ID, front );
						}

						if ( back == null || !back.IsValid() ) break;

						// Use the back solid as the new clipping target
						solid = back;
					}
					if ( !split ) continue;

					Delete( obj.ID );
				}
			}
			base.Perform( document );
		}
	}
}
