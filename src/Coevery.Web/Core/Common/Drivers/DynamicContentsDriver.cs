﻿using System.Collections.Generic;
using Coevery.ContentManagement;
using Coevery.ContentManagement.Drivers;
using Coevery.Core.Contents.Settings;

namespace Coevery.Core.Common.Drivers {
    public abstract class DynamicContentsDriver<TContent> : ContentPartDriver<TContent> where TContent : ContentPart, new() {
        protected override DriverResult Display(TContent part, string displayType, dynamic shapeHelper) {
            return Combined(
                ContentShape("Parts_Contents_Publish",
                    () => shapeHelper.Parts_Contents_Publish()),
                ContentShape("Parts_Contents_Publish_Summary",
                    () => shapeHelper.Parts_Contents_Publish_Summary()),
                ContentShape("Parts_Contents_Publish_SummaryAdmin",
                    () => shapeHelper.Parts_Contents_Publish_SummaryAdmin())
                );
        }

        protected override DriverResult Editor(TContent part, dynamic shapeHelper) {
            var results = new List<DriverResult> {ContentShape("Content_SaveButton", saveButton => saveButton)};

            if (part.TypeDefinition.Settings.GetModel<ContentTypeSettings>().Draftable) {
                results.Add(ContentShape("Content_PublishButton", publishButton => publishButton));
            }

            return Combined(results.ToArray());
        }

        protected override DriverResult Editor(TContent part, IUpdateModel updater, dynamic shapeHelper) {
            return Editor(part, updater);
        }
    }
}