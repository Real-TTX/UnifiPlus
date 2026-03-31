using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models;

public sealed class BandwidthTemplateSettingsRequest : IValidatableObject
{
    [Required]
    public string TemplateType { get; set; } = "Download";

    [Display(Name = "Template values")]
    public string TemplatesCsv { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.Equals(TemplateType, "Download", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(TemplateType, "Upload", StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult("The selected template type is invalid.", [nameof(TemplateType)]);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(TemplatesCsv))
        {
            yield return new ValidationResult("Enter at least one template value.", [nameof(TemplatesCsv)]);
            yield break;
        }

        var values = TemplatesCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (values.Length == 0)
        {
            yield return new ValidationResult("Enter at least one template value.", [nameof(TemplatesCsv)]);
            yield break;
        }

        foreach (var value in values)
        {
            if (!int.TryParse(value, out var parsed) || parsed < 1 || parsed > 100000)
            {
                yield return new ValidationResult("Template values must be whole numbers between 1 and 100000.", [nameof(TemplatesCsv)]);
                yield break;
            }
        }
    }
}
