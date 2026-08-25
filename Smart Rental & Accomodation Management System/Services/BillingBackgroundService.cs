namespace Smart_Rental___Accomodation_Management_System.Services
{
    public class BillingBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

        public BillingBackgroundService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);

            try
            {
                do
                {
                    await RunBillingPassAsync();
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
        }

        private async Task RunBillingPassAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var billingService = scope.ServiceProvider.GetRequiredService<BillingService>();

            await billingService.GenerateMonthlyInvoicesAsync();
            await billingService.FlagOverdueInvoicesAsync();
        }
    }
}
