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
        public override string InputScheme =>
            EzUISettings.Instance?.defaultPopUIInputScheme ?? "UI";

        /// <summary>
        /// 是否可以自动关闭,右键 ECS会进行自动关闭
        /// </summary>
        public virtual AutoCloseTopTypeEnum autoCloseTopType => AutoCloseTopTypeEnum.Hide;

        protected override void OnShow()
        {
            base.OnShow();
        }

        protected override void OnHide()
        {
            base.OnHide();
        }

        /// <summary>
        /// 获取自动关闭行为（重载以处理不同关闭原因）
        /// </summary>
        public override AutoCloseBehavior GetAutoCloseType(AutoCloseReason reason)
        {
            switch (reason)
            {
                case AutoCloseReason.MaskClick:
                    switch (maskClickOperation)
                    {
                        case MaskClickOperationType.None:
                            return AutoCloseBehavior.None;
                        case MaskClickOperationType.Hide:
                        case MaskClickOperationType.DirectHide:
                            return AutoCloseBehavior.Hide;
                        case MaskClickOperationType.Close:
                        case MaskClickOperationType.DirectClose:
                            return AutoCloseBehavior.Close;
                    }
                    return AutoCloseBehavior.None;

                case AutoCloseReason.PopAutoClose:
                    switch (autoCloseTopType)
                    {
                        case AutoCloseTopTypeEnum.None:
                        case AutoCloseTopTypeEnum.Ignore:
                            return AutoCloseBehavior.None;
                        case AutoCloseTopTypeEnum.Hide:
                            return AutoCloseBehavior.Hide;
                        case AutoCloseTopTypeEnum.Close:
                            return AutoCloseBehavior.Close;
                    }
                    return AutoCloseBehavior.None;

                default:
                    // MainUISwitch, DestroyAll 等使用基类默认行为
                    return base.GetAutoCloseType(reason);
            }
        }

        public virtual void OnMaskClick()
        {
            var useAnimation = maskClickOperation != MaskClickOperationType.DirectHide &&
                               maskClickOperation != MaskClickOperationType.DirectClose;
            UISystem?.AutoClose(this, AutoCloseReason.MaskClick, useAnimation);
        }
    }
}
