using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaycBillingWorker.Models
{
    [Table("BS_WaterReadingCityTap")]
    public class WaterReadingCityTap
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long BS_WaterReadingCityTapID { get; set; }

        // Foreign Key to BS_WaterReadingExport
        public long WaterReadingExportID { get; set; }

        //[ForeignKey("WaterReadingExportID")]
        //public virtual WaterReadingExport WaterReadingExport { get; set; }

        [Required]
        public DateTime CycleEndDateTime { get; set; }

        [Required]
        [StringLength(80)]
        [Column(TypeName = "varchar(80)")]
        public string UtilitySubscriberIdentifier { get; set; }

        // Nullable because the SQL allows NULL
        [StringLength(50)]
        [Column(TypeName = "char(50)")]
        public string? MeterSerialNumber { get; set; }

        // Nullable because the SQL allows NULL
        [StringLength(50)]
        [Column(TypeName = "char(50)")]
        public string? EndCycleMeterIndex { get; set; }
    }
}