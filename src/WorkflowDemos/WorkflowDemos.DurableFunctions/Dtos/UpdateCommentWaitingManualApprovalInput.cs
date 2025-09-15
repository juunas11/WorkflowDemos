namespace WorkflowDemos.DurableFunctions.Dtos;

public record UpdateCommentWaitingManualApprovalInput(string CommentId, string OrchestratorInstanceId);
