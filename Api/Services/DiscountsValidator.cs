using System;
using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;

namespace HappyTravel.Edo.Api.Services
{
    public static class DiscountsValidator
    {
        public static Result DiscountsDontExceedMarkups(List<decimal> discounts, Func<decimal, decimal> markupFunction)
        {
            // using dummy value here because markup function could multiply and add values 
            // and therefore its members cannot be compared directly to discounts which only multiply
            const decimal initial = 100m;
            var afterMarkup = markupFunction(initial);
            var afterDiscount = discounts.Aggregate(afterMarkup, (current, discount) => current * (100 - discount) / 100);
            return afterDiscount >= initial ? Result.Success() : Result.Failure("Discounts exceed markups");
        }
    }
}