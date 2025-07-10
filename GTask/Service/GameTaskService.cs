using Godot;
using GodotServiceFramework.Binding;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Db;
using GodotServiceFramework.GTask.Entity;
using SQLite;

namespace GodotServiceFramework.GTask.Service;

[InjectService]
public partial class GameTaskService
{
    public GameTaskService()
    {
        SqliteManager.Instance.AddTableType(typeof(GameTaskEntity));
    }


    public bool CreateGameTaskEntity(GameTaskEntity gameTaskEntity)
    {
        return SqliteManager.Instance.Insert(gameTaskEntity);
    }


    public bool DeleteGameTaskEntity(GameTaskEntity gameTaskEntity)
    {
        return SqliteManager.Instance.Delete(gameTaskEntity);
    }

    public bool UpdateGameTaskEntity(GameTaskEntity gameTaskEntity)
    {
        return SqliteManager.Instance.Update(gameTaskEntity);
    }

    public List<string> GetTaskGroups()
    {
        return SqliteManager.Table<GameTaskEntity>().Select(entity => entity.Group).Distinct().ToList();
    }

    [BindingCache]
    public List<GameTaskEntity> ListGameTaskEntityByGroup(string group)
    {
        return SqliteManager.Table<GameTaskEntity>().Where(entity => entity.Group == group).ToList();
    }
}