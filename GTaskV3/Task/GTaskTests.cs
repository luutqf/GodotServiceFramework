using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Db;
using GodotServiceFramework.GTaskV3.Entity;

namespace GodotServiceFramework.GTaskV3;

public class GTaskTests
{
    public static void Test01()
    {
        Console.WriteLine("sdfsdfs");

        var entity = SqliteManager.FindByName<GTaskSetEntity>("基础用例")!;

        var taskSet = new GTaskSet(new GTaskContext(), entity);

        taskSet.Start();


        // context.Start();
    }
}