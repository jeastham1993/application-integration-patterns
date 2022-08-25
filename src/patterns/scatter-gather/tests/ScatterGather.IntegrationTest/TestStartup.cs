using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScatterGather.IntegrationTest
{
    internal class TestStartup : IDisposable
    {
        private string stateMachineName = Environment.GetEnvironmentVariable("STATE_MACHINE_NAME") ?? "scatter-gather-orchestrator";
        public static AmazonStepFunctionsClient StepFunctionsClient { get; private set; }

        public static string StateMachineFunctionArn { get; private set; }

        public TestStartup()
        {
            StepFunctionsClient = new AmazonStepFunctionsClient();

            var stateMachines = StepFunctionsClient.ListStateMachinesAsync(new ListStateMachinesRequest()).Result;

            var stateMachine = stateMachines.StateMachines.FirstOrDefault(p => p.Name == stateMachineName);

            if (stateMachine == null)
            {
                throw new Exception($"State machine with name {stateMachineName} not found");
            }

            StateMachineFunctionArn = stateMachine.StateMachineArn;
        }

        public void Dispose()
        {
            // Do "global" teardown here; Only called once.
        }
    }
}
