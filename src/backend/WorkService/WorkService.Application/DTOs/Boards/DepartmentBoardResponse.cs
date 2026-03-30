namespace WorkService.Application.DTOs.Boards;

public class DepartmentBoardResponse
{
    public List<DepartmentBoardGroup> Departments { get; set; } = new();
}

public class DepartmentBoardGroup
{
    public string DepartmentName { get; set; } = string.Empty;
    public int TaskCount { get; set; }
    public int MemberCount { get; set; }
    public Dictionary<string, int> TasksByStatus { get; set; } = new();
}
