namespace WorkflowDemos.Temporal;

public class Comment
{
    public required string Text { get; set; }
    public string Id { get; set; } = "";
    public bool ApprovedByAi { get; set; }
    public bool ApprovedByHuman { get; set; }
}
