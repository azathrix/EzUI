using System;
using Azathrix.EzUI.Core;
using Azathrix.EzUI.Interfaces;
using UnityEngine;

namespace Azathrix.EzUI.Events
{
    // ----- 生命周期事件 -----
    public struct UIRootCreated
    {
        public Transform root;
        public Camera uiCamera;
    }

    public struct UIPanelCreated
    {
        public Panel panel;
        public string path;
    }

    public struct UIPanelDestroyed
    {
        public Panel panel;
        public string path;
    }

    public struct UIPanelStateChanged
    {
        public Panel panel;
        public Panel.StateEnum previous;
        public Panel.StateEnum current;
    }

    public struct UIPanelShow
    {
        public Panel panel;
        public string path;
    }

    public struct UIPanelShown
    {
        public Panel panel;
        public string path;
    }

    public struct UIPanelHide
    {
        public Panel panel;
        public string path;
    }

    public struct UIPanelHidden
    {
        public Panel panel;
        public string path;
    }

    public struct UIPanelClose
    {
        public Panel panel;
        public string path;
    }

    public struct UIFocusChanged
    {
        public IUIFocus previous;
        public IUIFocus current;
    }

    public struct UIMaskStateChanged
    {
        public bool active;
        public Panel target;
    }

    public struct UIInputSchemeChanged
    {
        public string previous;
        public string current;
        public int count;
        public Panel source;
    }

    /// <summary>
    /// UI 动画状态变化事件
    /// </summary>
    public struct UIAnimationStateChanged
    {
        /// <summary>
        /// 是否正在播放动画
        /// </summary>
        public bool isPlaying;

        /// <summary>
        /// 触发动画的 Panel
        /// </summary>
        public Panel source;

        /// <summary>
        /// 是否需要屏蔽输入
        /// </summary>
        public bool blockInput;
    }

    // ----- 事件化 API 请求 -----
    public struct UIShowRequest
    {
        public string path;
        public Type panelType;
        public bool useAnimation;
        public object userData;
    }

    public struct UIHideRequest
    {
        public string path;
        public Type panelType;
        public bool useAnimation;
    }

    public struct UICloseRequest
    {
        public string path;
        public Type panelType;
        public bool useAnimation;
    }

    public struct UIDestroyRequest
    {
        public string path;
        public Type panelType;
        public bool force;
    }

    public struct UIDestroyAllRequest
    {
        public bool force;
    }

    public struct UIShowOrHideRequest
    {
        public string path;
        public Type panelType;
        public bool useAnimation;
    }

    public struct UIShowMainRequest
    {
        public string path;
        public Type panelType;
        public bool useAnimation;
        public object userData;
    }

    public struct UISwitchMainRequest
    {
        public string path;
        public Type panelType;
        public bool useAnimation;
        public object userData;
    }

    public struct UILoadPersistenceRequest
    {
        public string path;
        public Type panelType;
    }

    public struct UISetPersistenceRequest
    {
        public string path;
        public Type panelType;
        public bool persistent;
    }

    public struct UIRefreshRequest
    {
    }
}
