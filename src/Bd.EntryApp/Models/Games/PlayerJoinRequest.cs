using System.ComponentModel.DataAnnotations;

namespace Bd.EntryApp.Models.Games;

public class PlayerJoinRequest
{
    [Required]
    public string? JoinCode { get; set; }
}