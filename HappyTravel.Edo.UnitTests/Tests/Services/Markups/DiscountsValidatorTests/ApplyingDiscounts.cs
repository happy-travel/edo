using System.Collections.Generic;
using HappyTravel.Edo.Api.Services;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Markups.DiscountsValidatorTests
{
    public class ApplyingDiscounts
    {
        [Fact]
        public void Should_fail_if_discounts_exceed()
        {
            var discounts = new List<decimal>{ 5m };
            decimal MarkupFunction(decimal amount) => amount * 1.04m;

            var result = DiscountsValidator.DiscountsDontExceedMarkups(discounts, MarkupFunction);
            
            Assert.True(result.IsFailure);
        }
        
        
        [Fact]
        public void Should_pass_if_discounts_dont_exceed()
        {
            var discounts = new List<decimal>{ 2, 1 };
            decimal MarkupFunction(decimal amount) => amount * 1.04m;

            var result = DiscountsValidator.DiscountsDontExceedMarkups(discounts, MarkupFunction);
            
            Assert.True(result.IsSuccess);
        }
    }
}