using Coevery.ContentManagement;

namespace Coevery.Core.Common.Models {
    public class BodyPart : ContentPart<BodyPartRecord> {
        public string Text {
            get { return Record.Text; }
            set { Record.Text = value; }
        }
    }
}
