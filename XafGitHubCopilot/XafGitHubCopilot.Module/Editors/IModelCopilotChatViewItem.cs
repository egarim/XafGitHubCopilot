using DevExpress.ExpressApp.Model;

namespace XafGitHubCopilot.Module.Editors
{
    /// <summary>
    /// Application Model interface for the Copilot Chat ViewItem.
    /// Both WinForms and Blazor platform-specific ViewItem classes
    /// reference this interface via <see cref="DevExpress.ExpressApp.Editors.ViewItemAttribute"/>
    /// so that a single model node name is shared across platforms.
    /// </summary>
    public interface IModelCopilotChatViewItem : IModelViewItem { }
}
