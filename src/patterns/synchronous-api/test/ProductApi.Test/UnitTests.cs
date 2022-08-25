using System;
using System.Linq;
using SynchronousApi.Core.DataTransfer;
using SynchronousApi.Core.Models;
using FluentAssertions;
using Xunit;

namespace ProductApi.Test
{
    public class UnitTests
    {
        [Fact]
        public void CanCreateProduct_ShouldSetProperties()
        {
            var product = Product.Create("Test product", 10);

            product.Name.Should().Be("Test product");
            product.Price.Should().Be(10);
            product.PricingHistory.Count.Should().Be(1);
            product.ProductId.Should().NotBeNull();
        }
        
        [Fact]
        public void CanCreateProduct_ShouldAllowDescriptionUpdate()
        {
            var product = Product.Create("Test product", 10);

            product.UpdateDescription("This is the description of my test product");

            product.Description.Should().Be("This is the description of my test product");
        }
        
        [Fact]
        public void CanCreateProduct_ShouldAllowPriceUpdateAndHistoryAdded()
        {
            var product = Product.Create("Test product", 10);

            product.UpdatePricing(15);

            product.Price.Should().Be(15);
            product.PricingHistory.Count.Should().Be(2);
            product.PricingHistory.FirstOrDefault().Price.Should().Be(10);
            product.PricingHistory.LastOrDefault().Price.Should().Be(15);
        }
        
        [Fact]
        public void CanCreateDataTransferObject_ShouldSetProperties()
        {
            var product = Product.Create("Test product", 10);
            product.UpdateDescription("This is the description of my test product");
            product.UpdatePricing(15);

            var dataTransfer = new ProductDTO(product);

            dataTransfer.ProductId.Should().Be(product.ProductId);
            dataTransfer.Name.Should().Be("Test product");
            dataTransfer.Description.Should().Be("This is the description of my test product");
            dataTransfer.CurrentPrice.Should().Be(15);
            dataTransfer.PricingHistory.Count.Should().Be(2);
            dataTransfer.PricingHistory.FirstOrDefault().Value.Should().Be(10);
        }
    }
}