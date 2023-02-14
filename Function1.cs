using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DurableTask.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace DurableTest56
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            // Replace "hello" with the name of your Durable Activity Function.
            outputs.Add(await context.CallActivityAsync<string>(nameof(PurgeHistory), "PurgeInformation"));
            
            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName(nameof(PurgeHistory))]
        public static async Task<string> PurgeHistory([ActivityTrigger] string name, IDurableOrchestrationClient durableClient, ILogger log)
        {
            var startedAt = DateTimeOffset.Now;

            log.LogInformation("{ActionName} started at {StartedAt}");

            AppConfig.PurgeOrchestrationsDaysToKeep = 30;
            
            var purgeResult = await durableClient.PurgeInstanceHistoryAsync(
                createdTimeFrom: DateTime.MinValue,
                createdTimeTo: DateTime.Now.AddDays(-1 * AppConfig.PurgeOrchestrationsDaysToKeep),
                runtimeStatus:
                    new List<OrchestrationStatus>
                    {
                    OrchestrationStatus.Completed,
                    OrchestrationStatus.Failed,
                    OrchestrationStatus.Terminated
                    }
                )
                .ConfigureAwait(false);

            var endedAt = DateTimeOffset.Now;

            log.LogInformation("{ActionName} purged {PurgeCount} orchestrations and ended at {EndedAt} ({Elapsed} elapsed)");

            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("Function1_HttpStart")]
        public static async Task Run([TimerTrigger("*/5 * * * * *")] TimerInfo myTimer,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Function1", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            //return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }

    internal class AppConfig
    {
        public static int PurgeOrchestrationsDaysToKeep { get; set; }
    }
}