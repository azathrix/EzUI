using System;
using Cysharp.Threading.Tasks;

namespace Azathrix.EzUI.Core
{
    public enum UIOperationType
    {
        Show,
        Hide,
        Close,
        Toggle,
        Destroy,
        DestroyAll
    }

    public enum UIOperationState
    {
        Pending,
        Running,
        Completed,
        Failed,
        Canceled
    }

    public sealed class UIOperationHandle
    {
        internal readonly UniTaskCompletionSource<Panel> completion = new UniTaskCompletionSource<Panel>();

        internal bool useAnimation;
        internal object userData;
        internal Panel target;
        internal bool force;

        internal UIOperationHandle(int id, UIOperationType type, string path)
        {
            this.id = id;
            this.type = type;
            this.path = path;
            state = UIOperationState.Pending;
        }

        public int id { get; }
        public UIOperationType type { get; }
        public string path { get; }

        public Panel panel { get; internal set; }
        public UIOperationState state { get; internal set; }
        public Exception exception { get; internal set; }

        public bool IsCompleted =>
            state == UIOperationState.Completed ||
            state == UIOperationState.Failed ||
            state == UIOperationState.Canceled;

        public UniTask<Panel> Task => completion.Task;

        public Panel TryGetPanel()
        {
            return state == UIOperationState.Completed ? panel : null;
        }

        public bool TryGetPanel(out Panel result)
        {
            result = state == UIOperationState.Completed ? panel : null;
            return result != null;
        }
    }
}
