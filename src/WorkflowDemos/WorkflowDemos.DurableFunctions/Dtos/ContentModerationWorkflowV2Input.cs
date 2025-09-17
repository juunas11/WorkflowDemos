namespace WorkflowDemos.DurableFunctions.Dtos;

public record ContentModerationWorkflowV2Input(List<Comment> Comments, bool DoManualReview);
