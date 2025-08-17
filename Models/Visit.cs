using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MinistryTracker.Models;             // For Student
using MinistryTracker.Models.Enums;       // For VisitType
public class Visit
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int StudentId { get; set; }

    [ForeignKey(nameof(StudentId))]
    public Student? Student { get; set; }

    [Required]
    public DateTime ScheduledDateTime { get; set; }

    [Required]
    public VisitStatus Status { get; set; } = VisitStatus.Scheduled;

    /// <summary>
    /// Visit-specific notes. Used to record what was discussed, what follow-up is needed,
    /// or any observations. Shown when reviewing the visit history.
    /// </summary>
    public string? Notes { get; set; } = string.Empty;

    public string? CancellationReason { get; set; }

    public VisitType VisitType { get; set; } = VisitType.ReturnVisit;
}
