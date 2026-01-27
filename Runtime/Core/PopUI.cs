using System;
using Azathrix.EzUI.Interfaces;
using UnityEngine.UI;

namespace Azathrix.EzUI.Core
{
    public enum AutoCloseTopTypeEnum
    {
        None,
        Ignore,
        Hide,
        Close,
    }

    /// <summary>
    /// 弹窗基类
    /// MainUI切换时默认关闭
    /// </summary>
    public class PopUI : FocusUI, IMaskClickable
    {
        public enum MaskClickOperationType
        {
            None,
            Hide,
            Close,
            DirectHide,
            DirectClose,
        }

        protected virtual MaskClickOperationType maskClickOperation => MaskClickOperationType.None;

        public override MainUIChangeBehavior mainUIChangeBehavior => MainUIChangeBehavior.Close;

        public Guid token { get; } = Guid.NewGuid();

        protected virtual bool autoPauseGame => true;

        /// <summary>
        /// 输入方案（为空则不切换）
        /// </summary>
        protected virtual string inputScheme =>
            EzUISettings.Instance?.defaultPopUIInputScheme ?? "UI";

        /// <summary>
        /// 是否可以自动关闭,右键 ECS会进行自动关闭
        /// </summary>
        public virtual AutoCloseTopTypeEnum autoCloseTopType => AutoCloseTopTypeEnum.Hide;

        protected override void OnShow()
        {
            base.OnShow();
            if (!string.IsNullOrWhiteSpace(inputScheme))
                UISystem?.SetInputScheme(this, inputScheme);
        }

        protected override void OnHide()
        {
            base.OnHide();
            if (!string.IsNullOrWhiteSpace(inputScheme))
                UISystem?.SetInputScheme(this, null);
        }

        /// <summary>
        /// 自动关闭时调用（重载以处理不同关闭原因）
        /// </summary>
        public override void OnAutoClose(AutoCloseReason reason, bool useAnimation = true)
        {
            switch (reason)
            {
                case AutoCloseReason.MaskClick:
                    switch (maskClickOperation)
                    {
                        case MaskClickOperationType.None:
                            break;
                        case MaskClickOperationType.Hide:
                            Hide(true);
                            break;
                        case MaskClickOperationType.Close:
                            Close(true);
                            break;
                        case MaskClickOperationType.DirectClose:
                            Close(false);
                            break;
                        case MaskClickOperationType.DirectHide:
                            Hide(false);
                            break;
                    }
                    break;

                case AutoCloseReason.PopAutoClose:
                    switch (autoCloseTopType)
                    {
                        case AutoCloseTopTypeEnum.None:
                        case AutoCloseTopTypeEnum.Ignore:
                            break;
                        case AutoCloseTopTypeEnum.Hide:
                            Hide(useAnimation);
                            break;
                        case AutoCloseTopTypeEnum.Close:
                            Close(useAnimation);
                            break;
                    }
                    break;

                default:
                    // MainUISwitch, DestroyAll 等使用基类默认行为
                    base.OnAutoClose(reason, useAnimation);
                    break;
            }
        }

        public virtual void OnMaskClick()
        {
            OnAutoClose(AutoCloseReason.MaskClick);
        }
    }
}
