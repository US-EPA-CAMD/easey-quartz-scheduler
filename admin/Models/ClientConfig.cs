using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
  [Table("client_config", Schema = "camdaux")]
  public class ClientConfig
  {
    [Key]
    [Column("client_id")]
    public string ClientId { get; set; }

    [Column("client_name")]
    public string ClientName { get; set; }

    [Column("support_email")]
    public string SupportEmail { get; set; }
  }
}
