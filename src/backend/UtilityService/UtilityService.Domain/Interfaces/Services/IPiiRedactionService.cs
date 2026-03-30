namespace UtilityService.Domain.Interfaces.Services;

public interface IPiiRedactionService
{
    string Redact(string input);
}
