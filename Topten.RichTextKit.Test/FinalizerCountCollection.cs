using Xunit;

namespace Topten.RichTextKit.Test
{
    // Tests that count objects pending finalization read a process-wide number, so
    // other test classes running in parallel would add to it.  Test classes in this
    // collection run on their own.
    [CollectionDefinition(Name, DisableParallelization = true)]
    public class FinalizerCountCollection
    {
        public const string Name = "FinalizerCount";
    }
}
