using Elsa.Workflows;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Matches;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Parameters;
using Microsoft.AspNetCore.Mvc;

namespace ElsaServer.Pages;

[Route("/resume")]
public class ResumeController(IWorkflowRuntime runtime, IWorkflowInstanceStore store) : Controller
{
    [HttpPost]
    public async void Resume([FromBody] string something)
    {
        var workflowInstances = await store.FindManyAsync(new WorkflowInstanceFilter
        {
            WorkflowStatus = WorkflowStatus.Running
        });
        var x = workflowInstances.ToArray().First();
        var res = await runtime.ResumeWorkflowAsync(
            "a97d942815c0d5c7",
            new ResumeWorkflowRuntimeParams
            {
                //CorrelationId = null,
                BookmarkId = "ID HERE",
                //ActivityId = null,
                //ActivityNodeId = null,
                //ActivityInstanceId = null,
                //ActivityHash = null,
                //Input = null,
                //Properties = null,
               // CancellationTokens = default
            });
    }
}