using WorkService.Domain.Helpers;

namespace WorkService.Tests.Helpers;

public class WorkflowStateMachineTests
{
    // --- Story transitions ---

    [Theory]
    [InlineData("Backlog", "Ready")]
    [InlineData("Ready", "InProgress")]
    [InlineData("InProgress", "InReview")]
    [InlineData("InReview", "InProgress")]
    [InlineData("InReview", "QA")]
    [InlineData("QA", "InProgress")]
    [InlineData("QA", "Done")]
    [InlineData("Done", "Closed")]
    public void IsValidStoryTransition_ValidTransitions_ReturnsTrue(string from, string to)
    {
        Assert.True(WorkflowStateMachine.IsValidStoryTransition(from, to));
    }

    [Theory]
    [InlineData("Backlog", "InProgress")]
    [InlineData("Backlog", "Done")]
    [InlineData("Ready", "Backlog")]
    [InlineData("InProgress", "Done")]
    [InlineData("InProgress", "QA")]
    [InlineData("Done", "InProgress")]
    [InlineData("Closed", "Backlog")]
    [InlineData("Closed", "Done")]
    [InlineData("QA", "Ready")]
    public void IsValidStoryTransition_InvalidTransitions_ReturnsFalse(string from, string to)
    {
        Assert.False(WorkflowStateMachine.IsValidStoryTransition(from, to));
    }

    [Fact]
    public void IsValidStoryTransition_UnknownStatus_ReturnsFalse()
    {
        Assert.False(WorkflowStateMachine.IsValidStoryTransition("Unknown", "Ready"));
    }

    // --- Task transitions ---

    [Theory]
    [InlineData("ToDo", "InProgress")]
    [InlineData("InProgress", "InReview")]
    [InlineData("InReview", "InProgress")]
    [InlineData("InReview", "Done")]
    public void IsValidTaskTransition_ValidTransitions_ReturnsTrue(string from, string to)
    {
        Assert.True(WorkflowStateMachine.IsValidTaskTransition(from, to));
    }

    [Theory]
    [InlineData("ToDo", "Done")]
    [InlineData("ToDo", "InReview")]
    [InlineData("InProgress", "ToDo")]
    [InlineData("InProgress", "Done")]
    [InlineData("Done", "InProgress")]
    [InlineData("Done", "ToDo")]
    public void IsValidTaskTransition_InvalidTransitions_ReturnsFalse(string from, string to)
    {
        Assert.False(WorkflowStateMachine.IsValidTaskTransition(from, to));
    }

    [Fact]
    public void IsValidTaskTransition_UnknownStatus_ReturnsFalse()
    {
        Assert.False(WorkflowStateMachine.IsValidTaskTransition("Unknown", "InProgress"));
    }

    [Fact]
    public void GetStoryTransitions_ReturnsAllStatuses()
    {
        var transitions = WorkflowStateMachine.GetStoryTransitions();
        Assert.Contains("Backlog", transitions.Keys);
        Assert.Contains("Ready", transitions.Keys);
        Assert.Contains("InProgress", transitions.Keys);
        Assert.Contains("InReview", transitions.Keys);
        Assert.Contains("QA", transitions.Keys);
        Assert.Contains("Done", transitions.Keys);
        Assert.Contains("Closed", transitions.Keys);
    }

    [Fact]
    public void GetTaskTransitions_ReturnsAllStatuses()
    {
        var transitions = WorkflowStateMachine.GetTaskTransitions();
        Assert.Contains("ToDo", transitions.Keys);
        Assert.Contains("InProgress", transitions.Keys);
        Assert.Contains("InReview", transitions.Keys);
        Assert.Contains("Done", transitions.Keys);
    }
}
