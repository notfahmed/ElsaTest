using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Services;
using Elsa.Workflows.UIHints;
using Elsa.Workflows.UIHints.Dropdown;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddElsa(elsa =>
{
    // Configure Management layer to use EF Core.
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UseSqlite()));

    // Configure Runtime layer to use EF Core.
    elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UseSqlite()));

    // Default Identity features for authentication/authorization.
    elsa.UseIdentity(identity =>
    {
        identity.TokenOptions =
            options => options.SigningKey =
                "sufficiently-large-secret-signing-key"; // This key needs to be at least 256 bits long.
        identity.UseAdminUserProvider();
    });

    // Configure ASP.NET authentication/authorization.
    elsa.UseDefaultAuthentication(auth => auth.UseAdminApiKey());

    // Expose Elsa API endpoints.
    elsa.UseWorkflowsApi();

    // Setup a SignalR hub for real-time updates from the server.
    elsa.UseRealTimeWorkflows();

    // Enable JavaScript workflow expressions.
    elsa.UseJavaScript();

    // Enable C# workflow expressions.
    elsa.UseCSharp();

    // Enable Liquid workflow expressions.
    elsa.UseLiquid();

    // Enable HTTP activities.
    elsa.UseHttp();

    // Use timer activities.
    elsa.UseScheduling();

    // Register custom activities from the application, if any.
    elsa.AddActivitiesFrom<Program>();

    // Register custom workflows from the application, if any.
    elsa.AddWorkflowsFrom<Program>();
    elsa.UseWorkflowManagement(management => { management.AddVariableType<ZDocument>("CRM"); });
});

// Configure CORS to allow designer app hosted on a different origin to invoke the APIs.
builder.Services.AddCors(cors => cors
    .AddDefaultPolicy(policy => policy
        .AllowAnyOrigin() // For demo purposes only. Use a specific origin instead.
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders(
            "x-elsa-workflow-instance-id"))); // Required for Elsa Studio in order to support running workflows from the designer. Alternatively, you can use the `*` wildcard to expose all headers.
builder.Services.AddScoped<IPropertyUIHandler, DocumentTypeHandler>();
builder.Services.AddScoped<IPropertyUIHandler, RefreshUiHandler>();
// Add Health Checks.
builder.Services.AddHealthChecks();

// Build the web application.
var app = builder.Build();

// Configure web application's middleware pipeline.
app.UseCors();
app.UseRouting(); // Required for SignalR.
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi(); // Use Elsa API endpoints.
app.UseWorkflows(); // Use Elsa middleware to handle HTTP requests mapped to HTTP Endpoint activities.
app.UseWorkflowsSignalRHubs(); // Optional SignalR integration. Elsa Studio uses SignalR to receive real-time updates from the server.

app.Run();

[Activity("ClaimPilotSum", "Custom", "Finds sum of two numbers")]
public class Sum : CodeActivity<int>
{
    public Sum()
    {
    } // Default constructor necessary in order to support JSON serialization.

    public Sum(Variable<int> a, Variable<int> b, Variable<int> result)
    {
        A = new Input<int>(a);
        B = new Input<int>(b);
        Result = new Output<int>(result);
    }

    [Input(Description = "The first number")]
    public Input<int> A { get; set; } = default!;

    [Input(Description = "The second number")]
    public Input<int> B { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var input1 = A.Get(context);
        var input2 = B.Get(context);
        var result = input1 + input2;
        context.SetResult(result);
    }
}

[Activity("ClaimPilotSum", "Custom", "Write a line of text to the console if given a variable")]
public class WriteVarLine : CodeActivity
{
    /// <inheritdoc />
    [JsonConstructor]
    public WriteVarLine([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) :
        base(source, line)
    {
    }


    /// <summary>
    ///     The variable to print.
    /// </summary>
    [Input(Description = "The variable to print.")]
    public Variable Variable { get; set; } = default!;

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        var res = Variable.Get(context);
        var provider = context.GetService<IStandardOutStreamProvider>() ?? new StandardOutStreamProvider(Console.Out);
        var textWriter = provider.GetTextWriter();
        textWriter.WriteLine($"The value of {Variable.Name}: {res}");
    }
}

[Activity("ClaimPilot", "Custom", "Send Doc")]
public class DocSender : CodeActivity<ZDocument>
{
    /// <inheritdoc />
    [JsonConstructor]
    public DocSender(Variable<ZDocument> document)
    {
        Document = new Input<ZDocument>(document);
    }

    /// <summary>
    ///     The doc to send.
    /// </summary>
    [Input(Description = "The doc to send.")]
    public Input<ZDocument> Document { get; set; } = default!;

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        var res = Document.Get(context);
        var provider = context.GetService<IStandardOutStreamProvider>() ?? new StandardOutStreamProvider(Console.Out);
        var textWriter = provider.GetTextWriter();
        textWriter.WriteLine($"Doc sent {res?.DocPk}, {res?.FileName}");
    }
}

public class ZDocument
{
    public int DocPk { get; set; }
    public string? FileName { get; set; }
}

internal class DocumentTypeHandler : DropDownOptionsProviderBase
{

    protected override ValueTask<ICollection<SelectListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        return new ValueTask<ICollection<SelectListItem>>([
            new SelectListItem("DOC", "1"),
            new SelectListItem("PDF", "2"),
            new SelectListItem("DOCX", "3"),
            new SelectListItem("XSL", "4")
        ]);
    }
}
public class RefreshUIHandler : IPropertyUIHandler
{
    public ValueTask<IDictionary<string, object>> GetUIPropertiesAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken = default)
    {
        IDictionary<string, object> result = new Dictionary<string, object>
        {
            { "Refresh", true }
        };
        return ValueTask.FromResult(result);
    }
}


internal class RefreshUiHandler : IPropertyUIHandler
{
    public ValueTask<IDictionary<string, object>> GetUIPropertiesAsync(PropertyInfo propertyInfo, object? context,
        CancellationToken cancellationToken = default)
    {
        IDictionary<string, object> result = new Dictionary<string, object>
        {
            { "Refresh", true }
        };
        return ValueTask.FromResult(result);
    }
}

[Activity("ClaimPilot", "Custom", "Select Doc")]
public class DocumentTypeSelectorActivity : CodeActivity
{
    [Input(
        Description = "The content type to use when sending the request.",
        UIHint = InputUIHints.DropDown,
        UIHandlers = [typeof(DocumentTypeHandler), typeof(RefreshUIHandler)]
    )]
    public Input<string> DocType { get; set; } = default!;

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        var provider = context.GetService<IStandardOutStreamProvider>() ?? new StandardOutStreamProvider(Console.Out);
        var textWriter = provider.GetTextWriter();
        textWriter.WriteLine($"Doc Type Selected :{DocType.Get(context)}");
    }
}