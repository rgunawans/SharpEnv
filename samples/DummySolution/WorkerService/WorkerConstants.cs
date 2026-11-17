namespace WorkerService;

public static class WorkerConstants
{
    public const string QueueName = "orders.created";
    public static readonly decimal DefaultTimeoutSeconds = 30.5m;
}
