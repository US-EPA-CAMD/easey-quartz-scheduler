using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("bulk_file_metadata", Schema = "camdaux")]
	public class BulkFileMetadata
	{
		[Key]
		[Column("file_name")]
 		public string FileName { get; set; }

		//[Key]
		[Column("s3_path")]
 		public string S3Path { get; set; }		 

		[Column("metadata")]
		public string Metadata { get; set; }

		[Column("file_size")]
		public long FileSize { get; set; }

		[Column("add_date")]
		public DateTime AddDate { get; set; }

		[Column("last_update_date")]
		public DateTime UpdateDate { get; set; }
	}
}
