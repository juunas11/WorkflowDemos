namespace WorkflowDemos.DurableFunctions.Dtos;

public class ManualModerationDecisionInput
{
    public required string InstanceId { get; set; }
    public required bool IsApproved { get; set; }
}