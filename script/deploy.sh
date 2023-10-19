#!/bin/sh
VERSION_REGEX="^([0-9]+)\.([0-9]+)\.([0-9]+)(-([0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*))?(\+([0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*))?$"

UNITY_PATH="/Applications/Unity/Hub/Editor/2020.3.36f1c1/Unity.app/Contents/MacOS/Unity"


# 从 Releases 下载
download() {
  releaseURL=$1
  upmPath=$2
  # Plugins
  zipFile=`basename "$releaseURL"`
  curl -L $releaseURL -o $zipFile
  unzip $zipFile -d $upmPath
  rm $zipFile
}

# 去掉依赖中的重复文件
diff() {
  srcPath=$1
  dstPath=$2
  for df in `ls $dstPath`:
  do
    for sf in `ls $srcPath`:
    do
      if cmp -s $dstPath/$df $srcPath/$sf
      then
        rm $dstPath/$df
        break
      fi
    done
  done
}

# 生成 package.json
package() {
  packageJson=$1
  upmPath=$2

  cat $packageJson | sed 's/__VERSION__/'$version'/' > $upmPath/package.json
}

# 生成 .meta
generateMetas() {
  upmPath=$1

  # 创建 Unity 工程
  unityProject=./Unity/UnityProject
  $UNITY_PATH -batchmode -quit -createProject $unityProject

  # 将 UPM 包移动到 Unity Project 下
  unityAssetsPath=$unityProject/Assets
  mv $upmPath/* $unityAssetsPath/

  # 使用 Unity Editor 打开工程，生成 .meta 文件
  $UNITY_PATH -batchmode -quit -nographics -silent-crashes -projectPath $unityProject

  mv $unityAssetsPath/* $upmPath/

  # 移除临时 Unity 工程
  rm -rf $unityProject
}

# 发布流程

version=$1

if [[ !($version =~ $VERSION_REGEX) ]]; then
  echo 'Invalid version'
  exit
fi

if !(test -f $UNITY_PATH); then
  echo 'Unity does NOT exist.'
  exit
fi

STORAGE_RELEASE_URL="https://github.com/leancloud/csharp-sdk/releases/download/$version/LeanCloud-SDK-Storage-Unity.zip"
REALTIME_RELEASE_URL="https://github.com/leancloud/csharp-sdk/releases/download/$version/LeanCloud-SDK-Realtime-Unity.zip"
PLAY_RELEASE_URL="https://github.com/leancloud/csharp-sdk/releases/download/$version/LeanCloud-SDK-Play-Unity.zip"

upmStoragePath="upm-storage"
upmRealtimePath="upm-realtime"
upmPlayPath="upm-play"

# 下载
download $STORAGE_RELEASE_URL $upmStoragePath
download $REALTIME_RELEASE_URL $upmRealtimePath
download $PLAY_RELEASE_URL $upmPlayPath

# 去重
diff $upmRealtimePath/Plugins $upmPlayPath/Plugins
diff $upmStoragePath/Plugins $upmRealtimePath/Plugins

# 打包
package ./Unity/storage.package.json $upmStoragePath
package ./Unity/realtime.package.json $upmRealtimePath
package ./Unity/play.package.json $upmPlayPath

# 生成 .meta
generateMetas $upmStoragePath
generateMetas $upmRealtimePath
generateMetas $upmPlayPath

# 发布 Github UPM
storageTag="storage"
realtimeTag="realtime"
playTag="play"

sh ./script/deploy-upm.sh $upmStoragePath $storageTag $version
sh ./script/deploy-upm.sh $upmRealtimePath $realtimeTag $version
sh ./script/deploy-upm.sh $upmPlayPath $playTag $version
 
# 发布 NPMJS
sh ./script/deploy-npm.sh $NPMJS_REGISTRY $NPMJS_TOKEN $upmStoragePath
sh ./script/deploy-npm.sh $NPMJS_REGISTRY $NPMJS_TOKEN $upmRealtimePath
sh ./script/deploy-npm.sh $NPMJS_REGISTRY $NPMJS_TOKEN $upmPlayPath

# 发布 Tap NPM
sh ./script/deploy-npm.sh $TAP_NPM_REGISTRY $TAP_NPM_TOKEN $upmStoragePath
sh ./script/deploy-npm.sh $TAP_NPM_REGISTRY $TAP_NPM_TOKEN $upmRealtimePath
sh ./script/deploy-npm.sh $TAP_NPM_REGISTRY $TAP_NPM_TOKEN $upmPlayPath

# 移除 SDK 包
rm -rf $upmStoragePath
rm -rf $upmRealtimePath
rm -rf $upmPlayPath
