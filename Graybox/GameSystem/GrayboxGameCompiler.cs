
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace Graybox.GameSystem;

public class GrayboxGameCompiler
{

	public static GrayboxGame Create( List<string> sourceFiles )
	{
		var syntaxTrees = sourceFiles.Select( file => CSharpSyntaxTree.ParseText( file ) ).ToList();

		var references = AppDomain.CurrentDomain.GetAssemblies()
			.Where( a => !a.IsDynamic && !string.IsNullOrWhiteSpace( a.Location ) )
			.Select( a => MetadataReference.CreateFromFile( a.Location ) )
			.Cast<MetadataReference>();

		var compilation = CSharpCompilation.Create(
			assemblyName: "DynamicAssembly",
			syntaxTrees: syntaxTrees,
			references: references,
			options: new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) );

		using ( var ms = new System.IO.MemoryStream() )
		{
			EmitResult result = compilation.Emit( ms );

			if ( !result.Success )
			{
				foreach ( var diagnostic in result.Diagnostics )
				{
					System.Diagnostics.Debug.WriteLine( $"{diagnostic.Id}: {diagnostic.GetMessage()}" );
				}
				return null;
			}

			ms.Seek( 0, System.IO.SeekOrigin.Begin );
			var assembly = Assembly.Load( ms.ToArray() );

			var baseGameSystemType = typeof( GrayboxGame );
			var derivedType = assembly.GetTypes().FirstOrDefault( t => t.IsSubclassOf( baseGameSystemType ) );

			if ( derivedType == null )
			{
				System.Diagnostics.Debug.WriteLine( "No type inheriting from BaseGameSystem was found." );
				return null;
			}

			return (GrayboxGame)Activator.CreateInstance( derivedType );
		}
	}

}
