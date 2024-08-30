
namespace Graybox.DataStructures.MapObjects;

public class Path
{
	public string Name { get; set; }
	public string Type { get; set; }
	public PathDirection Direction { get; set; }
	public List<PathNode> Nodes { get; private set; }

	public Path()
	{
		Nodes = new List<PathNode>();
	}

	public Path Clone()
	{
		Path p = new Path
		{
			Name = Name,
			Type = Type,
			Direction = Direction
		};
		foreach ( PathNode n in Nodes.Select( node => node.Clone() ) )
		{
			n.Parent = p;
			p.Nodes.Add( n );
		}
		return p;
	}
}
