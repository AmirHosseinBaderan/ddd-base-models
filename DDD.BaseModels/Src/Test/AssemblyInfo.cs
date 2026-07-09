using Xunit;

// The event handler tests rely on shared static counters, so run tests sequentially
// to keep those counters isolated per-test (each test resets them at the start).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
