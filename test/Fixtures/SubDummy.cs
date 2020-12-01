using System.Runtime.Serialization;

namespace PipServices3.Postgres.Persistence
{
	[DataContract]
	public class SubDummy
	{
		[DataMember(Name = "type")]
		public string Type { get; set; }

		[DataMember(Name = "array_of_double")]
		public double[] ArrayOfDouble { get; set; }
	}
}
