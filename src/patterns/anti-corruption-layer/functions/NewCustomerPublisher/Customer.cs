using System;
using System.Text.Json.Serialization;

namespace NewCustomerPublisher;

public class Customer
{
    [JsonConstructor]
    private Customer()
    {
    }

    public Customer(string emailAddress, string name)
    {
        this.Id = Guid.NewGuid().ToString();
        this.EmailAddress = emailAddress;
        this.Name = name;
    }
    public string Id { get; set; }
    public string EmailAddress { get; set; }
    public string Name { get; set; }
}