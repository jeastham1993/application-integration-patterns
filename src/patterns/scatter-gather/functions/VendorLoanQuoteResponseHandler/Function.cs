using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.XRay.Recorder.Core;
using Amazon.Lambda.CloudWatchEvents;
using System;
using VendorLoanQuoteResponseHandler.DataTransfer;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace VendorLoanQuoteResponseHandler
{
    public class Function
    {
        private readonly AmazonDynamoDBClient _dynamoDbClient;

        public Function() : this(null)
        {
        }

        internal Function(AmazonDynamoDBClient dynamoDbClient)
        {
            this._dynamoDbClient = dynamoDbClient ?? new AmazonDynamoDBClient();
        }

        public async Task FunctionHandler(CloudWatchEvent<LoanQuoteResult> inputEvent, ILambdaContext context)
        {
            context.Logger.LogLine($"Received response from {inputEvent.Detail.VendorName} with an interest rate of {inputEvent.Detail.InterestRate}");

            await this._dynamoDbClient.PutItemAsync(Environment.GetEnvironmentVariable("TABLE_NAME"), new Dictionary<string, AttributeValue>()
            {
                { "PK", new AttributeValue(inputEvent.Detail.Request.CorrelationId) },
                { "SK", new AttributeValue(inputEvent.Detail.VendorName) },
                { "InterestRate", new AttributeValue()
                    {
                        N = inputEvent.Detail.InterestRate.ToString("n2")
                    }
                } 
            });     
        }
    }
}
