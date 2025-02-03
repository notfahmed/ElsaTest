using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Models;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Requests;
using Microsoft.AspNetCore.Mvc;

namespace ElsaServer.Pages;

[Route("/resume")]
public class ResumeController(
    IWorkflowRuntime runtime,
    IWorkflowInstanceStore store,
    IWorkflowMatcher matcher,
    IWorkflowDispatcher dispatcher,
    IWorkflowInstanceManager manager) 
    : Controller
{
    [HttpPost]
    public async void Resume([FromBody] string something)
    {
        
        await dispatcher.DispatchAsync(
            new DispatchResumeWorkflowsRequest(
                "MyEventWorkflow",
                "MyEvent"),
            new DispatchWorkflowOptions());
        var bookmarks = await matcher.FindBookmarksAsync("MyEventWorkflow", "MyEvent");
        var bookmark = bookmarks.ToArray().First();
        var workflowInstances = await store.FindManyAsync(new WorkflowInstanceFilter
        {
            //Id = null,
            //Ids = null,
            //SearchTerm = null,
            DefinitionId = "MyEventWorkflow",
            //DefinitionVersionId = "MyEventWorkflow:1.0",
            // Version = null,
            //ParentWorkflowInstanceIds = null,
            //CorrelationId = null,
            //CorrelationIds = null,
            WorkflowStatus = WorkflowStatus.Running,
            WorkflowSubStatus = WorkflowSubStatus.Suspended,
            //HasIncidents = null,
           // IsSystem = null,
            //TimestampFilters = null
        });
        var workflow = workflowInstances.ToArray().First();
        
        var client = await runtime.CreateClientAsync();
        //client.WorkflowInstanceId = workflow.Id;
        var res = await client.RunInstanceAsync(new RunWorkflowInstanceRequest
        {
            TriggerActivityId = null,
            BookmarkId = workflow.WorkflowState.Bookmarks.First().Id,
            ActivityHandle = null,
            Properties = null,
            Input = null
        });
        // var res = await runtime.ResumeWorkflowAsync(
        //     workflow.Id,
        //     new ResumeWorkflowRuntimeParams
        //     {
        //         //CorrelationId = null,
        //         BookmarkId = workflow.WorkflowState.Bookmarks.First().Id,
        //         //ActivityId = null,
        //         //ActivityNodeId = null,
        //         //ActivityInstanceId = null,
        //         //ActivityHash = null,
        //         //Input = null,
        //         //Properties = null,
        //        // CancellationTokens = default
        //     });
    }
    // sprint todo Find out for sure if we need the workflowId and the bookmarkId in order to run a workflow again
    // sprint todo it seems like we DON'T need a matching bookmark payload
    // sprint todo Is there a better/easier way to lookup a specific workflow? - so far looks like we have to find it via the ID and/or status
}