namespace TdpGis.AdminApplication.AppModels;

public sealed class FormActionResult
{
    public Dictionary<string, string> FieldErrors { get; } = new(StringComparer.Ordinal);

    public string? SuccessMessage { get; set; }

    public string? CreatedAccessTokenPlain { get; set; }

    public Guid? RedirectGisEditId { get; set; }

    public string? ModelOnlyError { get; set; }

    public bool IsSuccess => FieldErrors.Count == 0 && string.IsNullOrEmpty(ModelOnlyError);

    public void AddFieldError(string fieldKey, string message)
    {
        FieldErrors[fieldKey] = message;
    }
}