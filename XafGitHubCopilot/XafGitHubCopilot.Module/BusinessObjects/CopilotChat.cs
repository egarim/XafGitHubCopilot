using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;

namespace XafGitHubCopilot.Module.BusinessObjects
{
    /// <summary>
    /// Non-persistent object that serves as the data source for the Copilot Chat Detail View.
    /// The Detail View layout will contain a <c>CopilotChatViewItem</c> that displays
    /// the DevExpress AI Chat control, wired to the GitHub Copilot SDK.
    /// </summary>
    [DomainComponent]
    [DefaultClassOptions]
    [DefaultProperty(nameof(Caption))]
    [ImageName("Actions_EnterGroup")]
    public class CopilotChat : NonPersistentBaseObject
    {
        public CopilotChat()
        {
            Caption = "Copilot Assistant";
        }

        [Browsable(false)]
        public string Caption { get; set; }
    }
}
