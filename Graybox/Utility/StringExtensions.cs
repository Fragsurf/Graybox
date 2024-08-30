
namespace Graybox;

public static class StringExtensions
{

	public static string NormalizePath( this string input )
	{
		return input.Replace( "\\", "/" ).TrimStart( '/' ).ToLower();
	}

	public static string[] SplitWithQuotes( this string line, Func<char, bool> splitTest = null, char quoteChar = '"' )
	{
		if ( splitTest == null ) splitTest = Char.IsWhiteSpace;
		List<string> result = new List<string>();
		int index = 0;
		bool inQuote = false;
		for ( int i = 0; i < line.Length; i++ )
		{
			char c = line[i];
			bool isSplitter = splitTest( c );
			if ( isSplitter && index == i )
			{
				index = i + 1;
			}
			else if ( c == quoteChar )
			{
				inQuote = !inQuote;
			}
			else if ( isSplitter && !inQuote )
			{
				result.Add( line.Substring( index, i - index ).Trim( quoteChar ) );
				index = i + 1;
			}
			if ( i != line.Length - 1 ) continue;
			result.Add( line.Substring( index, (i + 1) - index ).Trim( quoteChar ) );
		}
		return result.ToArray();
	}

	public static bool ToBool( this string Value )
	{
		string lowercaseValue = Value.ToLower();

		return lowercaseValue == "1" || lowercaseValue == "yes" || lowercaseValue == "true";
	}

}
