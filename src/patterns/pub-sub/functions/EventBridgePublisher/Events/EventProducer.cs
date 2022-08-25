using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;

namespace EventBridgePublisher;

public class EventProducer
{
    private readonly AmazonEventBridgeClient _eventBridgeClient;

    public EventProducer(AmazonEventBridgeClient eventBridgeClient)
    {
        this._eventBridgeClient = eventBridgeClient;
    }

    public async Task Publish(EventBase evt)
    {
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
                    Detail = JsonSerializer.Serialize(evt)
                }
            }
        });
    }
}