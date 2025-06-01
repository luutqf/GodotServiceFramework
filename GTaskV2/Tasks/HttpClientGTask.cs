using GodotServiceFramework.GTaskV2.Base;
using GodotServiceFramework.GTaskV2.Model;

namespace GodotServiceFramework.GTaskV2.Tasks;

/// <summary>
/// 发起一个http请求, 并验证状态码或者返回值
/// </summary>
public class HttpClientGTask(GTaskModel model, GTaskContext context) : BaseTimerGTask(model, context)
{
    // protected override Task Run()
    // {
    //     //1. 获取curl
    //     //2. 获取base64开关, 转换curl
    //     //3. 替换<application>为ip:port
    //     //4. 插入cookies
    //     //5. 执行curl
    //     //6. 解析返回值和状态码
    //     //7. 进行验证
    //     return Task.CompletedTask;
    // }

    protected override Task OnTimeout()
    {
        return Task.CompletedTask;
    }
}