namespace UtilityService.Domain.Interfaces.Services.PiiRedaction;

public interface IPiiRedactionService
{
    string Redact(string input);
}
