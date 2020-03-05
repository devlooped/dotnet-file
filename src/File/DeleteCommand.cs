using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.DotNet
{
    class DeleteCommand : Command
    {
        public DeleteCommand(Config configuration) : base(configuration) { }

        public override Task<int> ExecuteAsync()
        {
            Console.WriteLine("Delete");
            return Task.FromResult(0);
        }
    }
}
