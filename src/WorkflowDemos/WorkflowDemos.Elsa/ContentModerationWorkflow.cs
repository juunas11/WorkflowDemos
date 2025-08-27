using Elsa.Activities.ControlFlow;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Primitives;
using Elsa.Builders;

namespace WorkflowDemos.Elsa;

public class ContentModerationWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder builder)
    {
        builder
            .WithDisplayName("Content moderation workflow")
            .HttpEndpoint(activity => activity
                .WithPath("/start")
                .WithMethod("POST")
                .WithReadContent())
            .SetVariable("Comments", context => ((WorkflowInput)context.GetInput<HttpRequestModel>()!.Body!).Comments)
            .ParallelForEach(context => context.GetVariable<List<string>>("Comments")!, builder => builder
                .Then<Activities.CommentAiModerationActivity>("", setup => { setup. }, activity => activity
                    .When("")));
    }
}
public class WorkflowInput
{
    public required List<string> Comments { get; set; }
}