// CardStock.Worker — index construction, metric materialization, saved-screen
// evaluation, and session cleanup (D-039). No jobs in this slice; the project
// exists so the solution's shape is right from the first commit.
var builder = Host.CreateApplicationBuilder(args);

var host = builder.Build();
host.Run();
