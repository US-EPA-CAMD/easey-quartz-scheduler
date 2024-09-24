using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
  [Table("inventory_status_log", Schema = "camdaux")]
  public class JobLog
  {
    [Key]
    [Column("inventory_status_log_id")]
    public int InventoryStatusLogId { get; set; }

    [Column("fac_id")]
    public int FacId { get; set; }

    [Column("unit_id")]
    public int UnitId { get; set; }

    [Column("data_type_cd")]
    public string DataTypeCd { get; set; }

    [Column("userid")]
    public string UserId { get; set; }

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; }
  }
}
