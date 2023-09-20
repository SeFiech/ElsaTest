using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using ElsaTest;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

// https://v3.elsaworkflows.io/docs/guides/loading-workflows-from-json

var services = new ServiceCollection();

string[] pluginPaths = new string[]
{
    @"GreeterPlugin\bin\Debug\net7.0\GreeterPlugin.dll",    
};


string GetDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);


// Local Activity with the local Custom Activity
services.AddElsa(e => e.AddActivity<GreeterLocal>());

// Build service container.
var serviceProvider = services.BuildServiceProvider();


// Populate registries. This is only necessary for applications  that are not using hosted services.
await serviceProvider.PopulateRegistriesAsync();


// Import a workflow from a JSON file.
Console.WriteLine($"Load JSON File from {GetDirectory}\\HelloWorld.json");
var workflowJson = await File.ReadAllTextAsync(GetDirectory + "\\HelloWorld.json");


// Get a serializer to deserialize the workflow.
var serializer = serviceProvider.GetRequiredService<IActivitySerializer>();


// IMPORTANT! Before loading the workflow, we need to register the activity types.
var activityRegistry = serviceProvider.GetRequiredService<IActivityRegistry>();
var activityDescriber = serviceProvider.GetRequiredService<IActivityDescriber>();

var activityDescriptorTasks = pluginPaths.Select(async pluginPaths =>
{
    var pluginAssembly = LoadPlugin(pluginPaths);
    return await CreateActivityDescription(pluginAssembly, activityDescriber);
}).ToList();

var activityDescriptors = (await Task.WhenAll(activityDescriptorTasks)).SelectMany(x => x).ToList();


foreach (ActivityDescriptor actdesc in activityDescriptors)
{
    //activityRegistry.Clear();
    //activityRegistry.Add(typeof(IActivityDescriber), actdesc);
    activityRegistry.Register(actdesc);
}


var act = activityRegistry.ListAll();



// Deserialize the workflow.
var workflow = serializer.Deserialize<Workflow>(workflowJson);


// Resolve a workflow runner to run the workflow.
var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();


// Run the workflow.
await workflowRunner.RunAsync(workflow);





// Load Assembly
static Assembly LoadPlugin(string relativePath)
{
    // Navigate up to the solution root
    string root = Path.GetFullPath(Path.Combine(
        Path.GetDirectoryName(
            Path.GetDirectoryName(
                Path.GetDirectoryName(
                    Path.GetDirectoryName(
                        Path.GetDirectoryName(typeof(Program).Assembly.Location)))))));
    string pluginLocation = Path.GetFullPath(Path.Combine(root, relativePath.Replace('\\', Path.DirectorySeparatorChar)));
    Console.WriteLine($"Loading commands from: {pluginLocation}");
    PluginLoadContext loadContext = new PluginLoadContext(pluginLocation);
    return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
}



// Create ActivityDescriptor from Assembly
static async Task<IEnumerable<ActivityDescriptor>> CreateActivityDescription(Assembly assembly, IActivityDescriber describer)
{
    var descriptors = new List<ActivityDescriptor>();

    foreach (Type type in assembly.GetTypes())
    {
        if (typeof(IActivity).IsAssignableFrom(type))
        {            
            var ad = await describer.DescribeActivityAsync(type);
            descriptors.Add(ad);
        }
    }

    if (!descriptors.Any())
    {
        string availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
        throw new ApplicationException(
            $"Can't find any type which implement IActivity in {assembly} from {assembly.Location}.\n" +
            $"Available types: {availableTypes}");
    }

    return descriptors;
}