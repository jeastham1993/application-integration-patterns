using Amazon.DynamoDBv2.Model;
using ApplicationIntegrationPatterns.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;

namespace ApplicationIntegrationPatterns.Implementations
{
    public static class DynamoDbProductAdapter
    {
        public static Dictionary<string, AttributeValue> ProductToDynamoDbItem(Product product)
        {
            var item = new Dictionary<string, AttributeValue>(3);
            item.Add("PK", new AttributeValue(product.ProductId));
            item.Add("Name", new AttributeValue(product.Name));
            item.Add("Description", new AttributeValue(product.Description));
            item.Add("Price", new AttributeValue()
            {
                N = product.Price.ToString(CultureInfo.InvariantCulture)
            });

            var pricingHistory = new Dictionary<string, AttributeValue>();

            foreach (var history in product.PricingHistory)
            {
                pricingHistory.Add(history.Date.ToString("O"), new AttributeValue()
                {
                    N = history.Price.ToString(CultureInfo.InvariantCulture)
                });
            }

            item.Add("PricingHistory", new AttributeValue()
            {
                M = pricingHistory
            });

            return item;
        }

        public static Dictionary<string, AttributeValueUpdate> ProductToDynamoDbItemUpdate(Product product)
        {
            var item = new Dictionary<string, AttributeValueUpdate>(4);
            item.Add("Name", new AttributeValueUpdate()
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue(product.Name)
            });
            item.Add("Description", new AttributeValueUpdate()
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue(product.Description)
            });
            item.Add("Price", new AttributeValueUpdate()
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue()
                {
                    N = product.Price.ToString(CultureInfo.InvariantCulture)
                }
            });

            var pricingHistory = new Dictionary<string, AttributeValue>();

            foreach (var history in product.PricingHistory)
            {
                pricingHistory.Add(history.Date.ToString("O"), new AttributeValue()
                {
                    N = history.Price.ToString(CultureInfo.InvariantCulture)
                });
            }

            item.Add("PricingHistory", new AttributeValueUpdate()
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue()
                {
                    M = pricingHistory
                }
            });

            return item;
        }

        public static Product DynamoDbItemToProduct(Dictionary<string, AttributeValue> item)
        {
            var pricingHistory = new Dictionary<DateTime, decimal>();

            foreach (var priceHistory in item["PricingHistory"].M)
            {
                pricingHistory.Add(DateTime.Parse(priceHistory.Key), decimal.Parse(priceHistory.Value.N));
            }

            return Product.Create(item["PK"].S,
                item["Name"].S,
                decimal.Parse(item["Price"].N),
                item["Description"].S,
                pricingHistory);
        }
    }
}