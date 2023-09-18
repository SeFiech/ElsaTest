using Elsa.Workflows.Core;

namespace ElsaTest
{
    public class GreeterLocal : CodeActivity
    {
        protected override void Execute(ActivityExecutionContext context)
        {
            Console.WriteLine("Hello, world from GreeterLocal");
        }
    }
}
