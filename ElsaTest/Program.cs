using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using ElsaTest;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

// https://v3.elsaworkflows.io/docs/guides/loading-workflows-from-json

var services = new ServiceCollection();

string[] pluginPaths = new string[]
{
    @"GreeterPlugin\bin\Debug\net7.0\GreeterPlugin.dll",    
};


string GetDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);


//IEnumerable<IActivity> activities = pluginPaths.SelectMany(pluginPaths =>
//{
//    Assembly pluginAssembly = LoadPlugin(pluginPaths);
//    return CreateActivity(pluginAssembly);
//}).ToList();
//IActivity test = activities.First();


IEnumerable<Elsa.Workflows.Core.Models.ActivityDescriptor> activityDescribers = pluginPaths.SelectMany(pluginPaths =>
{
    Assembly pluginAssembly = LoadPlugin(pluginPaths);
    return CreateActivityDescription(pluginAssembly);
}).ToList();



// Local Activity 
services.AddElsa(e => e.AddActivity<GreeterLocal>());
//services.AddElsa();


// Build service container.
var serviceProvider = services.BuildServiceProvider();


// Populate registries. This is only necessary for applications  that are not using hosted services.
await serviceProvider.PopulateRegistriesAsync();


// Import a workflow from a JSON file.
Console.WriteLine($"Load JSON File from {GetDirectory}\\HelloWorld.json");
var workflowJson = await File.ReadAllTextAsync(GetDirectory + "\\HelloWorld.json");


// Get a serializer to deserialize the workflow.
var serializer = serviceProvider.GetRequiredService<IActivitySerializer>();

// Deserialize the workflow.
var workflow = serializer.Deserialize<Workflow>(workflowJson);




// Resolve a workflow runner to run the workflow.
var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();

//foreach (var activity in activities)
//{
//    ActivityFactory activityFactory = new();


//    ActivityDescriptor ad = new ActivityDescriptor
//    {
//        TypeName = activity.GetType().FullName,
//        Namespace = activity.GetType().Namespace,
//        DisplayName = activity.GetType().Name.Humanize(),
//        Category = "Console",
//        Name = activity.GetType().Name,
//        Version = activity.Version,

//        Constructor = context => {
//            return activity;
//        }

//        //Constructor =
//        //{
//        //    return activity.
//        //}
//        //Constructor = context =>
//        //{
//        //    //var act2 =  activityFactory.Create(activity.GetType(),context);
//        //    //act2.Type = activity.GetType().FullName;
//        //    //act2.ExecuteAsync = activity.ExecuteAsync();

//        //    return activity;
//        //}


//    };
//    //ad.Name = activity.Type.ToString();   


//    activityRegistry.Register(ad);
//    activityRegistry.
//    ActivityDescriptor activityDescriptor = activityRegistry.Find("ElsaTest.GreeterLocal");
//    //ActivityDescriptor activityDescriptor = activityRegistry.Find("GreeterPlugin.Greeter");
//    var temp = activityDescriptor.Constructor.Target;

//}


var activityRegistry = serviceProvider.GetRequiredService<IActivityRegistry>();

foreach (ActivityDescriptor actdesc in activityDescribers)
{
    //activityRegistry.Clear();
    //activityRegistry.Add(typeof(IActivityDescriber), actdesc);
    activityRegistry.Register(actdesc);
}


var act = activityRegistry.ListAll();


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



// Create IActivity from Assembly
static IEnumerable<IActivity> CreateActivity(Assembly assembly)
{
    int count = 0;

    foreach (Type type in assembly.GetTypes())
    {
        if (typeof(IActivity).IsAssignableFrom(type))
        {
            IActivity? result = Activator.CreateInstance(type) as IActivity;
            if (result != null)
            {
                count++;
                yield return result;
            }
        }
    }

    if (count == 0)
    {
        string availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
        throw new ApplicationException(
            $"Can't find any type which implement IActivity in {assembly} from {assembly.Location}.\n" +
            $"Available types: {availableTypes}");
    }
}


// Create ActivityDescriptor from Assembly
static IEnumerable<Elsa.Workflows.Core.Models.ActivityDescriptor> CreateActivityDescription(Assembly assembly)
{
    int count = 0;

    foreach (Type type in assembly.GetTypes())
    {
        if (typeof(IActivity).IsAssignableFrom(type))
        {
            ActivityDescriptor ad = new ActivityDescriptor
            {
                TypeName = type.FullName,
                Namespace = type.Namespace,
                DisplayName = type.Name.Humanize(),
                Category = "Console",
                Name = type.Name,
                Version = 1,

                Constructor = context => {
                    var ac = Activator.CreateInstance(type) as IActivity;
                    return ac;
                },                                
            };
            
            if (ad != null)
            {
                count++;                
            }

            yield return ad;
        }
    }

    if (count == 0)
    {
        string availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
        throw new ApplicationException(
            $"Can't find any type which implement IActivity in {assembly} from {assembly.Location}.\n" +
            $"Available types: {availableTypes}");
    }
}