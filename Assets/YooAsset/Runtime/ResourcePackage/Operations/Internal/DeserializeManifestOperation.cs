using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;

namespace YooAsset
{
    /// <summary>
    /// 反序列化清单文件操作
    /// </summary>
    internal class DeserializeManifestOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            RestoreFileData,
            DeserializeFileHeader,
            PrepareAssetList,
            DeserializeAssetList,
            PrepareBundleList,
            DeserializeBundleList,
            InitManifest,
            Done,
        }

        private readonly IManifestDecryptor _decryptor;
        private byte[] _sourceData;
        private BufferReader _buffer;
        private int _packageAssetCount;
        private int _packageBundleCount;
        private int _progressTotalValue;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 解析的清单实例
        /// </summary>
        public PackageManifest Manifest { get; private set; }

        public DeserializeManifestOperation(IManifestDecryptor decryptor, byte[] binaryData)
        {
            _decryptor = decryptor;
            _sourceData = binaryData;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.RestoreFileData;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.RestoreFileData)
            {
                if (_decryptor != null)
                {
                    var resultData = _decryptor.Decrypt(_sourceData);
                    if (resultData != null)
                        _sourceData = resultData;
                }

                _buffer = new BufferReader(_sourceData);
                _steps = ESteps.DeserializeFileHeader;
            }

            if (_steps == ESteps.DeserializeFileHeader)
            {
                if (_buffer.IsValid == false)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "Buffer is invalid.";
                    return;
                }

                // 读取文件标记
                uint fileSign = _buffer.ReadUInt32();
                if (fileSign != PackageManifestConsts.FileSignature)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "The manifest file format is invalid.";
                    return;
                }

                // 读取文件版本
                string fileVersion = _buffer.ReadUTF8();
                Version fileVer = new Version(fileVersion);
                Version ver2025_8_28 = new Version(PackageManifestConsts.VERSION_2025_8_28);
                Version ver2025_9_30 = new Version(PackageManifestConsts.VERSION_2025_9_30);
                if (fileVer < ver2025_8_28)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"The manifest version is lower than the minimum compatible version: {fileVer} < {ver2025_8_28}";
                    return;
                }

                // 读取文件头信息
                Manifest = new PackageManifest();
                Manifest.FileVersion = fileVersion;
                Manifest.EnableAddressable = _buffer.ReadBool();
                Manifest.SupportExtensionless = _buffer.ReadBool();
                Manifest.LocationToLower = _buffer.ReadBool();
                Manifest.IncludeAssetGUID = _buffer.ReadBool();
                if (fileVer >= ver2025_9_30)
                    Manifest.ReplaceAssetPathWithAddress = _buffer.ReadBool();
                else
                    Manifest.ReplaceAssetPathWithAddress = false;
                Manifest.OutputNameStyle = _buffer.ReadInt32();
                Manifest.BuildBundleType = _buffer.ReadInt32();
                Manifest.BuildPipeline = _buffer.ReadUTF8();
                Manifest.PackageName = _buffer.ReadUTF8();
                Manifest.PackageVersion = _buffer.ReadUTF8();
                Manifest.PackageNote = _buffer.ReadUTF8();

                // 检测配置
                if (Manifest.EnableAddressable && Manifest.LocationToLower)
                    throw new YooManifestException("Addressable mode does not support converting locations to lowercase.");
                if (Manifest.EnableAddressable == false && Manifest.ReplaceAssetPathWithAddress)
                    throw new YooManifestException("Replacing asset path with address requires Addressable to be enabled.");

                _steps = ESteps.PrepareAssetList;
            }

            if (_steps == ESteps.PrepareAssetList)
            {
                _packageAssetCount = _buffer.ReadInt32();
                _progressTotalValue = _packageAssetCount;
                CreateAssetCollection(Manifest, _packageAssetCount);
                _steps = ESteps.DeserializeAssetList;
            }
            if (_steps == ESteps.DeserializeAssetList)
            {
                bool replaceAssetPath = false;
                if (UnityEngine.Application.isPlaying)
                {
                    if (Manifest.EnableAddressable && Manifest.ReplaceAssetPathWithAddress)
                        replaceAssetPath = true;
                }

                while (_packageAssetCount > 0)
                {
                    var packageAsset = new PackageAsset();
                    packageAsset.Address = _buffer.ReadUTF8();
                    if (replaceAssetPath)
                    {
                        packageAsset.AssetPath = packageAsset.Address;
                        _buffer.SkipUTF8(); //跳过解析AssetPath
                    }
                    else
                    {
                        packageAsset.AssetPath = _buffer.ReadUTF8();
                    }
                    packageAsset.AssetGUID = _buffer.ReadUTF8();
                    packageAsset.AssetTags = _buffer.ReadUTF8Array();
                    packageAsset.BundleID = _buffer.ReadInt32();
                    packageAsset.DependentBundleIDs = _buffer.ReadInt32Array();
                    FillAssetCollection(Manifest, packageAsset, replaceAssetPath);

                    _packageAssetCount--;
                    Progress = 1f - (_packageAssetCount / (float)_progressTotalValue);
                    if (IsBusy)
                        break;
                }

                if (_packageAssetCount <= 0)
                {
                    _steps = ESteps.PrepareBundleList;
                }
            }

            if (_steps == ESteps.PrepareBundleList)
            {
                _packageBundleCount = _buffer.ReadInt32();
                _progressTotalValue = _packageBundleCount;
                CreateBundleCollection(Manifest, _packageBundleCount);
                _steps = ESteps.DeserializeBundleList;
            }
            if (_steps == ESteps.DeserializeBundleList)
            {
                while (_packageBundleCount > 0)
                {
                    var packageBundle = new PackageBundle();
                    packageBundle.BundleName = _buffer.ReadUTF8();
                    packageBundle.UnityCRC = _buffer.ReadUInt32();
                    packageBundle.FileHash = _buffer.ReadUTF8();
                    packageBundle.FileCRC = _buffer.ReadUInt32();
                    packageBundle.FileSize = _buffer.ReadInt64();
                    packageBundle.IsEncrypted = _buffer.ReadBool();
                    packageBundle.Tags = _buffer.ReadUTF8Array();
                    packageBundle.DependentBundleIDs = _buffer.ReadInt32Array();
                    FillBundleCollection(Manifest, packageBundle);

                    _packageBundleCount--;
                    Progress = 1f - (_packageBundleCount / (float)_progressTotalValue);
                    if (IsBusy)
                        break;
                }

                if (_packageBundleCount <= 0)
                {
                    _steps = ESteps.InitManifest;
                }
            }

            if (_steps == ESteps.InitManifest)
            {
                Manifest.Initialize();
                _steps = ESteps.Done;
                Status = EOperationStatus.Succeeded;
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }

        private void CreateAssetCollection(PackageManifest manifest, int assetCount)
        {
            manifest.AssetList = new List<PackageAsset>(assetCount);
            manifest.AssetDic = new Dictionary<string, PackageAsset>(assetCount);

            if (manifest.EnableAddressable)
            {
                manifest.AssetPathByLocation = new Dictionary<string, string>(assetCount * 3);
            }
            else
            {
                if (manifest.LocationToLower)
                    manifest.AssetPathByLocation = new Dictionary<string, string>(assetCount * 2, StringComparer.OrdinalIgnoreCase);
                else
                    manifest.AssetPathByLocation = new Dictionary<string, string>(assetCount * 2);
            }

            if (manifest.IncludeAssetGUID)
                manifest.AssetPathByAssetGUID = new Dictionary<string, string>(assetCount);
            else
                manifest.AssetPathByAssetGUID = new Dictionary<string, string>();
        }
        private void FillAssetCollection(PackageManifest manifest, PackageAsset packageAsset, bool replaceAssetPath)
        {
            // 添加到列表集合
            manifest.AssetList.Add(packageAsset);

            // 注意：我们不允许原始路径存在重名
            string assetPath = packageAsset.AssetPath;
            if (manifest.AssetDic.ContainsKey(assetPath))
                throw new YooManifestException($"Asset path already exists: {assetPath}");
            else
                manifest.AssetDic.Add(assetPath, packageAsset);

            // 填充AssetPathMapping1
            {
                string location = packageAsset.AssetPath;

                // 添加原生路径的映射
                if (manifest.AssetPathByLocation.ContainsKey(location))
                    throw new YooManifestException($"Location already exists: {location}");
                else
                    manifest.AssetPathByLocation.Add(location, packageAsset.AssetPath);

                // 添加无后缀名路径的映射
                if (manifest.SupportExtensionless)
                {
                    string locationWithoutExtension = Path.ChangeExtension(location, null);
                    if (ReferenceEquals(location, locationWithoutExtension) == false)
                    {
                        if (manifest.AssetPathByLocation.ContainsKey(locationWithoutExtension))
                            YooLogger.Warning($"Location already exists: {locationWithoutExtension}");
                        else
                            manifest.AssetPathByLocation.Add(locationWithoutExtension, packageAsset.AssetPath);
                    }
                }
            }

            // 填充AssetPathMapping2
            if (manifest.IncludeAssetGUID)
            {
                if (manifest.AssetPathByAssetGUID.ContainsKey(packageAsset.AssetGUID))
                    throw new YooManifestException($"Asset GUID already exists: {packageAsset.AssetGUID}");
                else
                    manifest.AssetPathByAssetGUID.Add(packageAsset.AssetGUID, packageAsset.AssetPath);
            }

            // 添加可寻址地址
            if (manifest.EnableAddressable && replaceAssetPath == false)
            {
                string location = packageAsset.Address;
                if (string.IsNullOrEmpty(location) == false)
                {
                    if (manifest.AssetPathByLocation.ContainsKey(location))
                        throw new YooManifestException($"Location already exists: {location}");
                    else
                        manifest.AssetPathByLocation.Add(location, packageAsset.AssetPath);
                }
            }
        }

        private void CreateBundleCollection(PackageManifest manifest, int bundleCount)
        {
            manifest.BundleList = new List<PackageBundle>(bundleCount);
            manifest.BundleByBundleName = new Dictionary<string, PackageBundle>(bundleCount);
            manifest.BundleByFileName = new Dictionary<string, PackageBundle>(bundleCount);
            manifest.BundleByBundleGUID = new Dictionary<string, PackageBundle>(bundleCount);
        }
        private void FillBundleCollection(PackageManifest manifest, PackageBundle packageBundle)
        {
            // 初始化资源包
            packageBundle.Initialize(manifest);

            // 添加到列表集合
            manifest.BundleList.Add(packageBundle);

            manifest.BundleByBundleName.Add(packageBundle.BundleName, packageBundle);
            manifest.BundleByFileName.Add(packageBundle.FileName, packageBundle);
            manifest.BundleByBundleGUID.Add(packageBundle.BundleGUID, packageBundle);
        }
    }
}