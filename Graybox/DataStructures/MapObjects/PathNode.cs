
namespace Graybox.DataStructures.MapObjects;

public class PathNode
{
	public Vector3 Position { get; set; }
	public int ID { get; set; }
	public string Name { get; set; }
	public List<Property> Properties { get; private set; }
	public Path Parent { get; set; }

	public PathNode()
	{
		Properties = new List<Property>();
	}

	public PathNode Clone()
	{
		PathNode node = new PathNode
		{
			Position = Position,
			ID = ID,
			Name = Name,
			Parent = Parent
		};
		node.Properties.AddRange( Properties.Select( x => x.Clone() ) );
		return node;
	}
}
