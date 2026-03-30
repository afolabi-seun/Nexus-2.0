namespace ProfileService.Domain.Helpers;

public static class DepartmentTypes
{
    public const string EngineeringName = "Engineering";
    public const string EngineeringCode = "ENG";

    public const string QAName = "QA";
    public const string QACode = "QA";

    public const string DevOpsName = "DevOps";
    public const string DevOpsCode = "DEVOPS";

    public const string ProductName = "Product";
    public const string ProductCode = "PROD";

    public const string DesignName = "Design";
    public const string DesignCode = "DESIGN";

    public static readonly (string Name, string Code)[] Defaults =
    [
        (EngineeringName, EngineeringCode),
        (QAName, QACode),
        (DevOpsName, DevOpsCode),
        (ProductName, ProductCode),
        (DesignName, DesignCode)
    ];
}
