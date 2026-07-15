using System.ComponentModel.DataAnnotations;

namespace VolksbankTracker.API.Models;

public record UpdateCategoryRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [StringLength(1000)] string Keywords = "",
    [StringLength(30)] string Color = "#6b7280",
    [StringLength(10)] string? Icon = null);
