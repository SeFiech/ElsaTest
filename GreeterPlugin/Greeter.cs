using Elsa.Workflows.Core;

namespace GreeterPlugin
{
    public class Greeter : CodeActivity
    {
        protected override void Execute(ActivityExecutionContext context)
        {
            Console.WriteLine("Hello World! from Greeter external");
        }       
    }
}