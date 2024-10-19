using DotNetConfig;

namespace Devlooped;

class ChangesCommand(Config configuration) : UpdateCommand(configuration)
{
    protected override bool DryRun => true;

    protected override AddCommand Clone() => new ChangesCommand(Configuration);
}
