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
        /// 是否可以自动关闭,右键 ECS会进行自动关闭
        /// </summary>
        public virtual AutoCloseTopTypeEnum autoCloseTopType => AutoCloseTopTypeEnum.Hide;

        protected virtual bool useUIEvent { get; } = true;

        protected override void OnCreate()
        {
            base.OnCreate();
            var btn = transform.Find("CloseBtn");
            if (btn)
            {
                btn.GetComponent<Button>().onClick.AddListener(() =>
                {
                    // TODO: 可在此触发关闭逻辑
                });
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            // TODO: 输入系统切换（按宏扩展）
        }

        protected override void OnHide()
        {
            base.OnHide();
            // TODO: 输入系统切换（按宏扩展）
        }

        protected override void OnScriptActivate()
        {
            base.OnScriptActivate();
            // TODO: 输入系统切换（按宏扩展）
        }

        protected override void OnScriptDeactivate()
        {
            base.OnScriptDeactivate();
            // TODO: 输入系统切换（按宏扩展）
        }

        public virtual void OnMaskClick()
        {
            switch (maskClickOperation)
            {
                case MaskClickOperationType.None:
                    break;
                case MaskClickOperationType.Hide:
                    Hide();
                    break;
                case MaskClickOperationType.Close:
                    Close();
                    break;
                case MaskClickOperationType.DirectClose:
                    Close(false);
                    break;
                case MaskClickOperationType.DirectHide:
                    Hide(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
