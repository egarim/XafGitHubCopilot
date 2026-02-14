using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Utils;
using DevExpress.AIIntegration.WinForms.Chat;
using XafGitHubCopilot.Module.Editors;

namespace XafGitHubCopilot.Win.Editors
{
    /// <summary>
    /// WinForms ViewItem that hosts the DevExpress <see cref="AIChatControl"/>.
    /// Messages are routed automatically through the registered <c>IChatClient</c>
    /// (backed by the GitHub Copilot SDK).
    /// </summary>
    [ViewItem(typeof(IModelCopilotChatViewItem))]
    public class CopilotChatViewItemWin : ViewItem
    {
        private AIChatControl _chatControl;

        public CopilotChatViewItemWin(IModelViewItem model, Type objectType)
            : base(objectType, model.Id)
        {
        }

        protected override object CreateControlCore()
        {
            _chatControl = new AIChatControl
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                UseStreaming = DefaultBoolean.True,
                ShowHeader = DefaultBoolean.True,
                HeaderText = "Copilot Assistant",
                EmptyStateText = "Ask me anything about your data. I'm powered by GitHub Copilot."
            };

            return _chatControl;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _chatControl != null)
            {
                _chatControl.Dispose();
                _chatControl = null;
            }
            base.Dispose(disposing);
        }
    }
}
