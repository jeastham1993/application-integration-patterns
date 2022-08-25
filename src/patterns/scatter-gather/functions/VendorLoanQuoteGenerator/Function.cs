using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.XRay.Recorder.Core;
using Amazon.Lambda.CloudWatchEvents;
using System;
using VendorLoanQuoteGenerator.DataTransfer;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;

namespace VendorLoanQuoteGenerator
{
    public class Function
    {
        private readonly Random _randomGenerator;
        private readonly AmazonEventBridgeClient _eventBridgeClient;

        public Function() : this(null, null)
        {
        }

        internal Function(Random random, AmazonEventBridgeClient eventBridgeClient)
        {
            this._randomGenerator = random ?? new Random();
            this._eventBridgeClient = eventBridgeClient ?? new AmazonEventBridgeClient();
        }

        public async Task FunctionHandler(CloudWatchEvent<GenerateLoanQuoteRequest> inputEvent, ILambdaContext context)
        {
            var multiplier = this._randomGenerator.Next(50, 100) / 10.00;

            var loanQuoteResult = new LoanQuoteResult
            {
                Request = inputEvent.Detail,
                InterestRate = multiplier,
                VendorName = Environment.GetEnvironmentVariable("VENDOR_NAME")
            };

            await this._eventBridgeClient.PutEventsAsync(new PutEventsRequest()
            {
                Entries = new List<PutEventsRequestEntry>
                {
                    new PutEventsRequestEntry
                    {
                        Detail = JsonSerializer.Serialize(loanQuoteResult),
                        DetailType = "com.vendors.completed-loan-request",
                        EventBusName = Environment.GetEnvironmentVariable("EVENT_BUS_NAME"),
                        Source = "com.vendors.quote-generator",
                        Time = DateTime.UtcNow,
                        Resources = new List<string> { inputEvent.Detail.CorrelationId }
                    }
                }
            });
        }
    }
}
