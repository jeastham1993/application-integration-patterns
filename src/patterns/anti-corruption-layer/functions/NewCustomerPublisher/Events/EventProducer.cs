using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using AWS.Lambda.Powertools.Logging;

namespace NewCustomerPublisher.Events;

public class EventProducer
{
    private readonly AmazonEventBridgeClient _eventBridgeClient;

    public EventProducer(AmazonEventBridgeClient eventBridgeClient)
    {
        this._eventBridgeClient = eventBridgeClient;
    }

    public async Task Publish(EventBase evt)
    {
        var evtPayload = JsonSerializer.Serialize(evt, evt.GetType());
        
        Logger.LogInformation("Publishing event data:");
        Logger.LogInformation(evtPayload);
        
        await this._eventBridgeClient.PutEventsAsync(new PutEventsRequest()
        {
            Entries = new List<PutEventsRequestEntry>()
            {
                new PutEventsRequestEntry()
                {
                    Source = evt.Source,
                    Time = DateTime.UtcNow,
                    EventBusName = Environment.GetEnvironmentVariable("EVENT_BUS_NAME"),
                    DetailType = evt.EventName,
                    Detail = evtPayload
                }
            }
        });
    }
}