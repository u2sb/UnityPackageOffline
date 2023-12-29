using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace U2SB.PackageOffline.Editor
{
  /// <summary>
  ///   本地化upm包
  /// </summary>
  public class PackageOffline : UnityEditor.Editor
  {
    public static string LocalPackageDir = Path.Combine(Application.dataPath, "../", "LocalPackages");

    /// <summary>
    ///   转换为本地包
    /// </summary>
    /// <returns></returns>
    [MenuItem("U2SB/PackageOffline/ConvertToLocal")]
    public static async UniTaskVoid ConvertToLocal()
    {
      Debug.Log("Wait...");

      if (!Directory.Exists(LocalPackageDir)) Directory.CreateDirectory(LocalPackageDir);

      var packagesDir = Path.Combine(LocalPackageDir, "../", "Packages");

      Debug.Log("正在备份文件...");

      File.Copy(Path.Combine(packagesDir, "manifest.json"),
        Path.Combine(packagesDir, $"manifest-{DateTime.Now.Ticks}.json"));

      Debug.Log("正在重新解析包...");
      Client.Resolve();

      await UniTask.WaitForSeconds(1);

      var a = Client.List(false, true);
      while (!a.IsCompleted) await UniTask.Yield();

      if (a.Status == StatusCode.Success)
      {
        var pathList = new List<string>();

        foreach (var s in a.Result)
          if (s.source is PackageSource.Registry or PackageSource.Git)
          {
            var path = Path.Combine(LocalPackageDir, $"{s.name}-{s.version}.tgz");
            path = new Uri(path).LocalPath;
            pathList.Add($"file:{path}");
            if (File.Exists(path)) File.Delete(path);
            Debug.Log($"正在创建本地包 {s.name}...");
            var c = Client.Pack(s.resolvedPath, LocalPackageDir);
            while (!c.IsCompleted) await UniTask.Yield();
          }

        await UniTask.WaitForSeconds(1);

        Debug.Log("正在更新 manifest.json...");
        var e = Client.AddAndRemove(pathList.ToArray());
        while (!e.IsCompleted) await UniTask.Yield();
        await UniTask.WaitForSeconds(1);

        Debug.Log("正在重新解析包...");
        Client.Resolve();
      }

      Debug.Log("Completed");
    }
  }
}