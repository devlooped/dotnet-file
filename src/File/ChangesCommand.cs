﻿using DotNetConfig;

namespace Devlooped
{
    class ChangesCommand : UpdateCommand
    {
        public ChangesCommand(Config configuration) : base(configuration) { }

        protected override bool DryRun => true;

        protected override AddCommand Clone() => new ChangesCommand(Configuration);
    }
}
