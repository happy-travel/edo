﻿using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace HappyTravel.Edo.Api.Conventions
{
    public class LocalizationConvention : IApplicationModelConvention
    {
        public void Apply(ApplicationModel application)
        {
            var culturePrefix = new AttributeRouteModel(new RouteAttribute("{culture}"));

            foreach (var controller in application.Controllers.Where(c => c.DisplayName != "Microsoft.AspNet.OData.MetadataController (Microsoft.AspNetCore.OData)"))
            {
                var matchedSelectors = controller.Selectors.Where(x => x.AttributeRouteModel != null).ToList();
                if (matchedSelectors.Any())
                {
                    foreach (var selectorModel in matchedSelectors)
                        selectorModel.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(culturePrefix,
                            selectorModel.AttributeRouteModel);
                }

                var unmatchedSelectors = controller.Selectors.Where(x => x.AttributeRouteModel == null).ToList();
                if (!unmatchedSelectors.Any())
                    continue;

                foreach (var selectorModel in unmatchedSelectors)
                    selectorModel.AttributeRouteModel = culturePrefix;
            }
        }
    }
}