﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace YooAsset
{
	internal class HostPlayModeImpl : IPlayModeServices, IBundleServices
	{
		private PatchManifest _activePatchManifest;

		// 参数相关
		private string _packageName;
		private bool _locationToLower;
		private string _defaultHostServer;
		private string _fallbackHostServer;
		private IQueryServices _queryServices;

		/// <summary>
		/// 异步初始化
		/// </summary>
		public InitializationOperation InitializeAsync(string packageName, bool locationToLower, string defaultHostServer, string fallbackHostServer, IQueryServices queryServices)
		{
			_packageName = packageName;
			_locationToLower = locationToLower;
			_defaultHostServer = defaultHostServer;
			_fallbackHostServer = fallbackHostServer;
			_queryServices = queryServices;

			var operation = new HostPlayModeInitializationOperation(this, packageName);
			OperationSystem.StartOperation(operation);
			return operation;
		}

		// WEB相关
		public string GetPatchDownloadMainURL(string fileName)
		{
			return $"{_defaultHostServer}/{fileName}";
		}
		public string GetPatchDownloadFallbackURL(string fileName)
		{
			return $"{_fallbackHostServer}/{fileName}";
		}

		// 下载相关
		private List<BundleInfo> ConvertToDownloadList(List<PatchBundle> downloadList)
		{
			List<BundleInfo> result = new List<BundleInfo>(downloadList.Count);
			foreach (var patchBundle in downloadList)
			{
				var bundleInfo = ConvertToDownloadInfo(patchBundle);
				result.Add(bundleInfo);
			}
			return result;
		}
		private BundleInfo ConvertToDownloadInfo(PatchBundle patchBundle)
		{
			string remoteMainURL = GetPatchDownloadMainURL(patchBundle.FileName);
			string remoteFallbackURL = GetPatchDownloadFallbackURL(patchBundle.FileName);
			BundleInfo bundleInfo = new BundleInfo(patchBundle, BundleInfo.ELoadMode.LoadFromRemote, remoteMainURL, remoteFallbackURL);
			return bundleInfo;
		}

		// 解压相关
		private List<BundleInfo> ConvertToUnpackList(List<PatchBundle> unpackList)
		{
			List<BundleInfo> result = new List<BundleInfo>(unpackList.Count);
			foreach (var patchBundle in unpackList)
			{
				var bundleInfo = ConvertToUnpackInfo(patchBundle);
				result.Add(bundleInfo);
			}
			return result;
		}
		public static BundleInfo ConvertToUnpackInfo(PatchBundle patchBundle)
		{
			// 注意：我们把流加载路径指定为远端下载地址
			string streamingPath = PathHelper.ConvertToWWWPath(patchBundle.StreamingFilePath);
			BundleInfo bundleInfo = new BundleInfo(patchBundle, BundleInfo.ELoadMode.LoadFromStreaming, streamingPath, streamingPath);
			return bundleInfo;
		}

		#region IPlayModeServices接口
		public PatchManifest ActivePatchManifest
		{
			set
			{
				_activePatchManifest = value;
				_activePatchManifest.InitAssetPathMapping(_locationToLower);
			}
			get
			{
				return _activePatchManifest;
			}
		}
		public string GetPackageVersion()
		{
			if (_activePatchManifest == null)
				return string.Empty;
			return _activePatchManifest.PackageVersion;
		}
		public bool IsBuildinPatchBundle(PatchBundle patchBundle)
		{
			return _queryServices.QueryStreamingAssets(patchBundle.FileName);
		}

		UpdatePackageVersionOperation IPlayModeServices.UpdatePackageVersionAsync(bool appendTimeTicks, int timeout)
		{
			var operation = new HostPlayModeUpdatePackageVersionOperation(this, _packageName, appendTimeTicks, timeout);
			OperationSystem.StartOperation(operation);
			return operation;
		}
		UpdatePackageManifestOperation IPlayModeServices.UpdatePackageManifestAsync(string packageVersion, bool autoSaveManifest, bool autoActiveManifest, int timeout)
		{
			var operation = new HostPlayModeUpdatePackageManifestOperation(this, _packageName, packageVersion, autoSaveManifest, autoActiveManifest, timeout);
			OperationSystem.StartOperation(operation);
			return operation;
		}
		CheckPackageContentsOperation IPlayModeServices.CheckPackageContentsAsync()
		{
			var operation = new HostPlayModeCheckPackageContentsOperation(this, _packageName);
			OperationSystem.StartOperation(operation);
			return operation;
		}

		PatchDownloaderOperation IPlayModeServices.CreatePatchDownloaderByAll(int downloadingMaxNumber, int failedTryAgain, int timeout)
		{
			List<BundleInfo> downloadList = GetDownloadListByAll();
			var operation = new PatchDownloaderOperation(downloadList, downloadingMaxNumber, failedTryAgain, timeout);
			return operation;
		}
		private List<BundleInfo> GetDownloadListByAll()
		{
			List<PatchBundle> downloadList = new List<PatchBundle>(1000);
			foreach (var patchBundle in _activePatchManifest.BundleList)
			{
				// 忽略缓存文件
				if (CacheSystem.IsCached(patchBundle))
					continue;

				// 忽略APP资源
				if (IsBuildinPatchBundle(patchBundle))
					continue;

				downloadList.Add(patchBundle);
			}

			return ConvertToDownloadList(downloadList);
		}

		PatchDownloaderOperation IPlayModeServices.CreatePatchDownloaderByTags(string[] tags, int downloadingMaxNumber, int failedTryAgain, int timeout)
		{
			List<BundleInfo> downloadList = GetDownloadListByTags(tags);
			var operation = new PatchDownloaderOperation(downloadList, downloadingMaxNumber, failedTryAgain, timeout);
			return operation;
		}
		private List<BundleInfo> GetDownloadListByTags(string[] tags)
		{
			List<PatchBundle> downloadList = new List<PatchBundle>(1000);
			foreach (var patchBundle in _activePatchManifest.BundleList)
			{
				// 忽略缓存文件
				if (CacheSystem.IsCached(patchBundle))
					continue;

				// 忽略APP资源
				if (IsBuildinPatchBundle(patchBundle))
					continue;

				// 如果未带任何标记，则统一下载
				if (patchBundle.HasAnyTags() == false)
				{
					downloadList.Add(patchBundle);
				}
				else
				{
					// 查询DLC资源
					if (patchBundle.HasTag(tags))
					{
						downloadList.Add(patchBundle);
					}
				}
			}

			return ConvertToDownloadList(downloadList);
		}

		PatchDownloaderOperation IPlayModeServices.CreatePatchDownloaderByPaths(AssetInfo[] assetInfos, int downloadingMaxNumber, int failedTryAgain, int timeout)
		{
			List<BundleInfo> downloadList = GetDownloadListByPaths(assetInfos);
			var operation = new PatchDownloaderOperation(downloadList, downloadingMaxNumber, failedTryAgain, timeout);
			return operation;
		}
		private List<BundleInfo> GetDownloadListByPaths(AssetInfo[] assetInfos)
		{
			// 获取资源对象的资源包和所有依赖资源包
			List<PatchBundle> checkList = new List<PatchBundle>();
			foreach (var assetInfo in assetInfos)
			{
				if (assetInfo.IsInvalid)
				{
					YooLogger.Warning(assetInfo.Error);
					continue;
				}

				// 注意：如果补丁清单里未找到资源包会抛出异常！
				PatchBundle mainBundle = _activePatchManifest.GetMainPatchBundle(assetInfo.AssetPath);
				if (checkList.Contains(mainBundle) == false)
					checkList.Add(mainBundle);

				// 注意：如果补丁清单里未找到资源包会抛出异常！
				PatchBundle[] dependBundles = _activePatchManifest.GetAllDependencies(assetInfo.AssetPath);
				foreach (var dependBundle in dependBundles)
				{
					if (checkList.Contains(dependBundle) == false)
						checkList.Add(dependBundle);
				}
			}

			List<PatchBundle> downloadList = new List<PatchBundle>(1000);
			foreach (var patchBundle in checkList)
			{
				// 忽略缓存文件
				if (CacheSystem.IsCached(patchBundle))
					continue;

				// 忽略APP资源
				if (IsBuildinPatchBundle(patchBundle))
					continue;

				downloadList.Add(patchBundle);
			}

			return ConvertToDownloadList(downloadList);
		}

		PatchUnpackerOperation IPlayModeServices.CreatePatchUnpackerByTags(string[] tags, int upackingMaxNumber, int failedTryAgain, int timeout)
		{
			List<BundleInfo> unpcakList = GetUnpackListByTags(tags);
			var operation = new PatchUnpackerOperation(unpcakList, upackingMaxNumber, failedTryAgain, timeout);
			return operation;
		}
		private List<BundleInfo> GetUnpackListByTags(string[] tags)
		{
			List<PatchBundle> downloadList = new List<PatchBundle>(1000);
			foreach (var patchBundle in _activePatchManifest.BundleList)
			{
				// 忽略缓存文件
				if (CacheSystem.IsCached(patchBundle))
					continue;

				// 查询DLC资源
				if (IsBuildinPatchBundle(patchBundle))
				{
					if (patchBundle.HasTag(tags))
					{
						downloadList.Add(patchBundle);
					}
				}
			}

			return ConvertToUnpackList(downloadList);
		}

		PatchUnpackerOperation IPlayModeServices.CreatePatchUnpackerByAll(int upackingMaxNumber, int failedTryAgain, int timeout)
		{
			List<BundleInfo> unpcakList = GetUnpackListByAll();
			var operation = new PatchUnpackerOperation(unpcakList, upackingMaxNumber, failedTryAgain, timeout);
			return operation;
		}
		private List<BundleInfo> GetUnpackListByAll()
		{
			List<PatchBundle> downloadList = new List<PatchBundle>(1000);
			foreach (var patchBundle in _activePatchManifest.BundleList)
			{
				// 忽略缓存文件
				if (CacheSystem.IsCached(patchBundle))
					continue;

				if (IsBuildinPatchBundle(patchBundle))
				{
					downloadList.Add(patchBundle);
				}
			}

			return ConvertToUnpackList(downloadList);
		}
		#endregion

		#region IBundleServices接口
		private BundleInfo CreateBundleInfo(PatchBundle patchBundle)
		{
			if (patchBundle == null)
				throw new Exception("Should never get here !");

			// 查询沙盒资源
			if (CacheSystem.IsCached(patchBundle))
			{
				BundleInfo bundleInfo = new BundleInfo(patchBundle, BundleInfo.ELoadMode.LoadFromCache);
				return bundleInfo;
			}

			// 查询APP资源
			if (IsBuildinPatchBundle(patchBundle))
			{
				BundleInfo bundleInfo = new BundleInfo(patchBundle, BundleInfo.ELoadMode.LoadFromStreaming);
				return bundleInfo;
			}

			// 从服务端下载
			return ConvertToDownloadInfo(patchBundle);
		}
		BundleInfo IBundleServices.GetBundleInfo(AssetInfo assetInfo)
		{
			if (assetInfo.IsInvalid)
				throw new Exception("Should never get here !");

			// 注意：如果补丁清单里未找到资源包会抛出异常！
			var patchBundle = _activePatchManifest.GetMainPatchBundle(assetInfo.AssetPath);
			return CreateBundleInfo(patchBundle);
		}
		BundleInfo[] IBundleServices.GetAllDependBundleInfos(AssetInfo assetInfo)
		{
			if (assetInfo.IsInvalid)
				throw new Exception("Should never get here !");

			// 注意：如果补丁清单里未找到资源包会抛出异常！
			var depends = _activePatchManifest.GetAllDependencies(assetInfo.AssetPath);
			List<BundleInfo> result = new List<BundleInfo>(depends.Length);
			foreach (var patchBundle in depends)
			{
				BundleInfo bundleInfo = CreateBundleInfo(patchBundle);
				result.Add(bundleInfo);
			}
			return result.ToArray();
		}
		AssetInfo[] IBundleServices.GetAssetInfos(string[] tags)
		{
			return _activePatchManifest.GetAssetsInfoByTags(tags);
		}
		PatchAsset IBundleServices.TryGetPatchAsset(string assetPath)
		{
			if (_activePatchManifest.TryGetPatchAsset(assetPath, out PatchAsset patchAsset))
				return patchAsset;
			else
				return null;
		}
		string IBundleServices.MappingToAssetPath(string location)
		{
			return _activePatchManifest.MappingToAssetPath(location);
		}
		string IBundleServices.TryMappingToAssetPath(string location)
		{
			return _activePatchManifest.TryMappingToAssetPath(location);
		}
		string IBundleServices.GetPackageName()
		{
			return _packageName;
		}
		bool IBundleServices.IsIncludeBundleFile(string fileName)
		{
			return _activePatchManifest.IsIncludeBundleFile(fileName);
		}
		bool IBundleServices.IsServicesValid()
		{
			return _activePatchManifest != null;
		}
		#endregion
	}
}