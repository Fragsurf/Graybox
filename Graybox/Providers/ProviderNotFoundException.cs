using System;

namespace Graybox.Providers
{
	public class ProviderNotFoundException : ProviderException
	{
		public ProviderNotFoundException( string message, Exception innerException ) : base( message, innerException )
		{
		}

		public ProviderNotFoundException() : base( "A suitable provider was not able to be found." )
		{
		}

		public ProviderNotFoundException( string message ) : base( message )
		{
		}
	}
}
