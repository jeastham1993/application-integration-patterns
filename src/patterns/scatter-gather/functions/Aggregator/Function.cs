using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.CloudWatchEvents;
using System;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Aggregator.DataTransfer;

namespace Aggregator
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

        public async Task<List<AggregationResult>> FunctionHandler(GenerateLoanQuoteRequest request, ILambdaContext context)
        {
            var queryResults = await this._dynamoDbClient.QueryAsync(new QueryRequest()
            {
                TableName = Environment.GetEnvironmentVariable("TABLE_NAME"),
                KeyConditionExpression = "PK = :pk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    { ":pk", new AttributeValue(request.CorrelationId )}
                }
            });

            var results = new List<AggregationResult>();

            foreach (var item in queryResults.Items)
            {
                results.Add(new AggregationResult()
                {
                    VendorName = item["SK"].S,
                    InterestRate = double.Parse(item["InterestRate"].N)
                });
            }

            return results;
        }
    }
}
