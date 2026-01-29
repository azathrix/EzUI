using System.Threading;
using Azathrix.EzUI.Core;
using Azathrix.EzUI.Interfaces;
using Cysharp.Threading.Tasks;

namespace Azathrix.EzUI.Tests
{
    public class TestPanel : Panel
    {
    }

    public class TestMainUIA : Panel, IMainUI
    {
        public static void Reset()
        {
        }
    }

    public class TestMainUIB : Panel, IMainUI
    {
        public static void Reset()
        {
        }
    }

    public class TestMainUIHide : Panel, IMainUI
    {
        public override MainUIChangeBehavior mainUIChangeBehavior => MainUIChangeBehavior.Hide;

        public static void Reset()
        {
        }
    }

    public class TestMainUIClose : Panel, IMainUI
    {
        public override MainUIChangeBehavior mainUIChangeBehavior => MainUIChangeBehavior.Close;

        public static void Reset()
        {
        }
    }

    public class TestMainUINone : Panel, IMainUI
    {
        public override MainUIChangeBehavior mainUIChangeBehavior => MainUIChangeBehavior.None;

        public static void Reset()
        {
        }
    }

    public class TestLoadableMainUI : Panel, IMainUI, IMainUILoadable
    {
        public static int onLoadingCount;

        public static void Reset()
        {
            onLoadingCount = 0;
        }

        public LoadingConfig LoadingConfig => new LoadingConfig
        {
            loadingType = "test",
            initialTitle = "title",
            initialText = "text"
        };

        public async UniTask OnLoading(ILoadingController controller)
        {
            onLoadingCount++;
            controller?.SetProgress(1f);
            controller?.SetTitle("done");
            controller?.SetText("done");
            await UniTask.Yield();
        }

    }

    public class TestPersistentPanel : Panel
    {
    }

    public class TestPopUI : PopUI
    {
        public override string InputScheme => "UI";
    }

    public class TestPopUIAlt : PopUI
    {
        public override string InputScheme => "UI2";
    }

    public class TestMaskClickPopUI : PopUI
    {
        protected override MaskClickOperationType maskClickOperation => MaskClickOperationType.Hide;
        public override string InputScheme => "UI";
    }

    public class TestAutoCloseNonePanel : Panel
    {
        public override AutoCloseBehavior GetAutoCloseType(AutoCloseReason reason)
        {
            if (reason == AutoCloseReason.MainUISwitch)
                return AutoCloseBehavior.None;
            return base.GetAutoCloseType(reason);
        }
    }

    public class TestReentrantPanel : Panel
    {
        public static UIOperationHandle lastHandle;
        public static bool didTrigger;

        public static void Reset()
        {
            lastHandle = null;
            didTrigger = false;
        }

        protected override void OnShow()
        {
            base.OnShow();
            if (didTrigger || UISystem == null)
                return;
            didTrigger = true;
            lastHandle = UISystem.Show<TestPanel>(false);
        }
    }

    public class TestAnimatedPanel : Panel
    {
        public static int showAnimCount;
        public static int hideAnimCount;

        public static void Reset()
        {
            showAnimCount = 0;
            hideAnimCount = 0;
        }

        protected override async UniTask ShowAnimationAsync(CancellationToken cancellationToken)
        {
            showAnimCount++;
            await UniTask.Delay(30, cancellationToken: cancellationToken);
        }

        protected override async UniTask HideAnimationAsync(CancellationToken cancellationToken)
        {
            hideAnimCount++;
            await UniTask.Delay(30, cancellationToken: cancellationToken);
        }
    }

    public class TestSequencePanel : Panel
    {
        public static bool showStarted;
        public static bool showCompleted;
        public static bool hideStarted;
        public static bool hideCompleted;
        public static bool hideStartedAfterShow;

        public static void Reset()
        {
            showStarted = false;
            showCompleted = false;
            hideStarted = false;
            hideCompleted = false;
            hideStartedAfterShow = false;
        }

        protected override async UniTask ShowAnimationAsync(CancellationToken cancellationToken)
        {
            showStarted = true;
            await UniTask.Delay(30, cancellationToken: cancellationToken);
            showCompleted = true;
        }

        protected override async UniTask HideAnimationAsync(CancellationToken cancellationToken)
        {
            hideStarted = true;
            hideStartedAfterShow = showCompleted;
            await UniTask.Delay(30, cancellationToken: cancellationToken);
            hideCompleted = true;
        }
    }

    public class TestLifecyclePanel : Panel
    {
        public static int onCreateCount;
        public static int onShowCount;
        public static int onShownCount;
        public static int onHideCount;
        public static int onHiddenCount;
        public static int onCloseCount;
        public static int onClosedCount;

        public static void Reset()
        {
            onCreateCount = 0;
            onShowCount = 0;
            onShownCount = 0;
            onHideCount = 0;
            onHiddenCount = 0;
            onCloseCount = 0;
            onClosedCount = 0;
        }

        protected override void OnCreate()
        {
            onCreateCount++;
        }

        protected override void OnShow()
        {
            onShowCount++;
        }

        protected override void OnShown()
        {
            onShownCount++;
        }

        protected override void OnHide()
        {
            onHideCount++;
        }

        protected override void OnHidden()
        {
            onHiddenCount++;
        }

        protected override void OnClose()
        {
            onCloseCount++;
        }

        protected override void OnClosed()
        {
            onClosedCount++;
        }
    }

    public class TestLifecycleView : View
    {
        public static int onCreateCount;
        public static int onShowCount;
        public static int onShownCount;
        public static int onHideCount;
        public static int onHiddenCount;

        public static void Reset()
        {
            onCreateCount = 0;
            onShowCount = 0;
            onShownCount = 0;
            onHideCount = 0;
            onHiddenCount = 0;
        }

        public override void OnCreate()
        {
            onCreateCount++;
        }

        public override void OnShow()
        {
            onShowCount++;
        }

        public override void OnShown()
        {
            onShownCount++;
        }

        public override void OnHide()
        {
            onHideCount++;
        }

        public override void OnHidden()
        {
            onHiddenCount++;
        }
    }
}
