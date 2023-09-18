using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;

namespace GreeterPlugin
{
    public class Greeter : CodeActivity
    {
        protected override void Execute(ActivityExecutionContext context)
        {
            Console.WriteLine("Hello, world! from Greeter external");
        }
    }
}