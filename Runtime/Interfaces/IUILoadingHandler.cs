using Cysharp.Threading.Tasks;

namespace Azathrix.EzUI.Interfaces
{
    /// <summary>
    /// Loading 控制器，由 MainUI 传递给 LoadingHandler 进行控制
    /// </summary>
    public interface ILoadingController
    {
        /// <summary>
        /// 设置进度 (0-1)
        /// </summary>
        void SetProgress(float progress);

        /// <summary>
        /// 设置文本
        /// </summary>
        void SetText(string text);

        /// <summary>
        /// 设置标题
        /// </summary>
        void SetTitle(string title);
    }

    /// <summary>
    /// Loading 配置
    /// </summary>
    public struct LoadingConfig
    {
        /// <summary>
        /// Loading 类型/ID（用于区分不同样式的 Loading）
        /// </summary>
        public string loadingType;

        /// <summary>
        /// 初始标题
        /// </summary>
        public string initialTitle;

        /// <summary>
        /// 初始文本
        /// </summary>
        public string initialText;

        /// <summary>
        /// 默认配置
        /// </summary>
        public static LoadingConfig Default => new LoadingConfig
        {
            loadingType = "default",
            initialTitle = "",
            initialText = ""
        };
    }

    /// <summary>
    /// UI Loading 处理器接口
    /// 用于在主UI切换时显示/隐藏Loading
    /// </summary>
    public interface IUILoadingHandler
    {
        /// <summary>
        /// 显示 Loading
        /// </summary>
        /// <param name="config">Loading 配置</param>
        /// <returns>Loading 控制器</returns>
        UniTask<ILoadingController> ShowLoading(LoadingConfig config);

        /// <summary>
        /// 隐藏 Loading
        /// </summary>
        UniTask HideLoading();
    }

    /// <summary>
    /// 可加载的 MainUI 接口
    /// 实现此接口的 MainUI 在切换时会显示 Loading
    /// </summary>
    public interface IMainUILoadable
    {
        /// <summary>
        /// Loading 配置（可重载以指定不同的 Loading 类型）
        /// </summary>
        LoadingConfig LoadingConfig { get; }

        /// <summary>
        /// 加载过程（在 Loading 显示期间执行）
        /// </summary>
        /// <param name="controller">Loading 控制器，用于更新进度和文本</param>
        UniTask OnLoading(ILoadingController controller);
    }
}
