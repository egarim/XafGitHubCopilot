using System;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using XafGitHubCopilot.Module.Editors;

namespace XafGitHubCopilot.Blazor.Server.Editors.CopilotChatViewItem
{
    /// <summary>
    /// Blazor ViewItem that hosts the DevExpress <c>DxAIChat</c> component.
    /// Messages are routed automatically through the registered <c>IChatClient</c>
    /// (backed by the GitHub Copilot SDK).
    /// </summary>
    [ViewItem(typeof(IModelCopilotChatViewItem))]
    public class CopilotChatViewItemBlazor : ViewItem, IComponentContentHolder
    {
        public CopilotChatViewItemBlazor(IModelViewItem model, Type objectType)
            : base(objectType, model.Id)
        {
        }

        RenderFragment IComponentContentHolder.ComponentContent => builder =>
        {
            builder.OpenComponent<CopilotChat>(0);
            builder.CloseComponent();
        };

        protected override object CreateControlCore()
        {
            // In Blazor, IComponentContentHolder.ComponentContent is used for rendering.
            // Return a placeholder object to satisfy the ViewItem contract.
            return new object();
        }
    }
}
