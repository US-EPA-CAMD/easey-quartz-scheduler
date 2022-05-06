using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("bulk_file_metadata", Schema = "camdaux")]
	public class BulkFileMetadata
	{

		[Key]
		[Column("bulk_file_id")]
 		public int Id { get; set; }

		[Column("s3_key")]
 		public string S3Key { get; set; }		 

		[Column("metadata")]
		public string Metadata { get; set; }
	}
}
