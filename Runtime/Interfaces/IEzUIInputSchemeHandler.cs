namespace Azathrix.EzUI
{
    /// <summary>
    /// 输入方案处理器接口（由外部输入系统实现）
    /// </summary>
    public interface IEzUIInputSchemeHandler
    {
        void ApplyInputScheme(string previous, string current, object source);
    }
}
