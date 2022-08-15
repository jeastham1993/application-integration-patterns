using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using System.Text.Json;
using Xunit.Abstractions;

namespace ScatterGather.IntegrationTest;

public class IntegrationTests : IClassFixture<TestStartup>
{
    private readonly ITestOutputHelper output;

    public IntegrationTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public async Task TestStepFunctionInvoke_ShouldReturnQuotes()
    {
        var invokeResult = await TestStartup.StepFunctionsClient.StartExecutionAsync(new StartExecutionRequest()
        {
            StateMachineArn = TestStartup.StateMachineFunctionArn,
            Input = "{  \"CustomerId\": \"James Eastham\",  \"CorrelationId\": \"235423523001\",  \"LoanAmount\": 567.98}"
        });

        var isRunning = true;
        DescribeExecutionResponse executionDetail = null;

        while (isRunning)
        {
            this.output.WriteLine("Checking execution status...");

            executionDetail = await TestStartup.StepFunctionsClient.DescribeExecutionAsync(new DescribeExecutionRequest()
            {
                ExecutionArn = invokeResult.ExecutionArn
            });

            this.output.WriteLine($"Execution status is {executionDetail.Status}");

            if (executionDetail.Status != ExecutionStatus.RUNNING)
            {
                isRunning = false;
            }
        }

        var result = JsonSerializer.Deserialize<List<LoanQuoteResponse>>(executionDetail.Output);

        Assert.Equal(3, result.Count);
    }
}

public class LoanQuoteResponse
{
    public string VendorName { get; set; }

    public double InterestRate { get; set; }
}
