namespace VRAtlas.Filters;

public class FilterValidationResponse
{
    public string Error = "Validation Failed";

    public IEnumerable<string> Errors { get; }

    public FilterValidationResponse(IEnumerable<string> errors)
    {
        Errors = errors;
    }
}