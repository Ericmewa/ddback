using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NCBA.DCL.DTOs
{
    public class SubmitToRMRequest
    {
        [Required]
        [MinLength(1)]
        [JsonPropertyName("documents")]
        public List<ChecklistCategoryDto> Documents { get; set; } = new();
    }
}
