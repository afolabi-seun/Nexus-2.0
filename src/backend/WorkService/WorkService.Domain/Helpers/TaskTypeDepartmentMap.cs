using WorkService.Domain.Exceptions;

namespace WorkService.Domain.Helpers;

public static class TaskTypeDepartmentMap
{
    private static readonly Dictionary<string, string> Map = new()
    {
        ["Development"] = "ENG",
        ["Testing"] = "QA",
        ["DevOps"] = "DEVOPS",
        ["Design"] = "DESIGN",
        ["Documentation"] = "PROD",
        ["Bug"] = "ENG"
    };

    public static string GetDepartmentCode(string taskType)
        => Map.TryGetValue(taskType, out var code) ? code : throw new InvalidTaskTypeException(taskType);

    public static IReadOnlyDictionary<string, string> GetAll() => Map;
}
