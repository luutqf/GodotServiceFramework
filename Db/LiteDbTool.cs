// using GodotServiceFramework.Util;
// using LiteDB;
//
// namespace GodotServiceFramework.Db;
//
// public static class LiteDbTool
// {
//     private static readonly Dictionary<string, LiteDatabase> LiteDbConnections = [];
//
//     public static LiteDatabase Db(string path)
//     {
//         if (LiteDbConnections.TryGetValue(path, out var db))
//         {
//             return db;
//         }
//
//         //TODO 这里重复了，可以提取
//         var directoryName = Path.GetDirectoryName(path);
//
//         if (directoryName == null)
//         {
//             throw new Exception("path not valid");
//         }
//
//         if (!Directory.Exists(directoryName))
//         {
//             FileUtils.CreateDirectoryWithCheck(directoryName);
//         }
//
//         Log.Info($"databasePath: {path}");
//
//         db = new LiteDatabase(path);
//         LiteDbConnections[path] = db;
//         return db;
//     }
//
//     public static bool InsertItem<T>(this LiteDatabase @this, T entity) where T : class
//     {
//         try
//         {
//             return @this.GetCollection<T>().Insert(entity) != null;
//         }
//         catch (Exception e)
//         {
//             Log.Error($"Insert error: {e.Message}");
//             return false;
//         }
//     }
//     
//     
//
//     public static void Clear(string path)
//     {
//         LiteDbConnections[path].Dispose();
//         LiteDbConnections.Remove(path);
//     }
//
//     public static void ClearAll()
//     {
//         foreach (var liteDatabase in LiteDbConnections.Values)
//         {
//             liteDatabase.Dispose();
//         }
//
//         LiteDbConnections.Clear();
//     }
// }