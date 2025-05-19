using Godot;
using GodotServiceFramework.Binding;
using GodotServiceFramework.Context.Service;
using GodotServiceFramework.Db;
using GodotServiceFramework.GTask.Entity;
using SQLite;

namespace GodotServiceFramework.GTask.Service;

public partial class GameTaskService : AutoGodotService
{
    private readonly SQLiteConnection _db;

    public GameTaskService()
    {
        var globalizePath = ProjectSettings.GlobalizePath("user://data/db.sqlite");
        _db = SqliteTool.Db(globalizePath, out _, initTables: [typeof(GameTaskEntity)]);
    }



    public override void Destroy()
    {
    }

    public bool CreateGameTaskEntity(GameTaskEntity gameTaskEntity)
    {
        return _db.InsertItem(gameTaskEntity);
    }


    public bool DeleteGameTaskEntity(GameTaskEntity gameTaskEntity)
    {
        return _db.DeleteItem(gameTaskEntity);
    }

    public bool UpdateGameTaskEntity(GameTaskEntity gameTaskEntity)
    {
        return _db.UpdateItem(gameTaskEntity);
    }

    public List<string> GetTaskGroups()
    {
        return _db.Table<GameTaskEntity>().Select(entity => entity.Group).Distinct().ToList();
    }

    [BindingCache]
    public List<GameTaskEntity> ListGameTaskEntityByGroup(string group)
    {
        return _db.Table<GameTaskEntity>().Where(entity => entity.Group == group).ToList();
    }
}