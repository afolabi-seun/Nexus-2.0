namespace BillingService.Application.DTOs.Admin;

public record PaginatedResponse<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
