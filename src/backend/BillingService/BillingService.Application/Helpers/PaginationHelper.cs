namespace BillingService.Application.Helpers;

public static class PaginationHelper
{
    public static void Normalize(ref int page, ref int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
    }
}
