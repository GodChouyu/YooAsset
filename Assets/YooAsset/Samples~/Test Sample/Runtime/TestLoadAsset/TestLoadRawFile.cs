using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using UnityEngine.TestTools;
using NUnit.Framework;
using YooAsset;

public class TestLoadRawFile
{
    public IEnumerator RuntimeTester()
    {
        ResourcePackage package = YooAssets.GetPackage(TestDefine.RawBundlePackageName);
        Assert.IsNotNull(package);

        // 测试异步加载
        {
            var rawFileHandle = package.LoadRawFileAsync("raw_file_a");
            yield return rawFileHandle;
            Assert.AreEqual(EOperationStatus.Succeed, rawFileHandle.Status);

            var filePath = rawFileHandle.GetRawFilePath();
            Assert.IsNotNull(filePath);
        }

        // 测试同步加载
        {
            var rawFileHandle = package.LoadRawFileSync("raw_file_b");
            Assert.AreEqual(EOperationStatus.Succeed, rawFileHandle.Status);

            var filePath = rawFileHandle.GetRawFilePath();
            Assert.IsNotNull(filePath);
        }
    }
}