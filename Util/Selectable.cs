namespace GodotServiceFramework.Util;

public interface ISelectable
{
    /// <summary>
    /// 可选择的组件
    /// </summary>
    /// <param name="confirm"></param>
    void Select(bool confirm);
}