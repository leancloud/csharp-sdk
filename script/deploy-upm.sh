#!/bin/sh
VERSION_REGEX="^[0-9]+(\.[0-9]+){2}$"

version=$1

STORAGE_RELEASE_URL="https://github.com/leancloud/csharp-sdk/releases/download/$version/LeanCloud-SDK-Storage-Unity.zip"
REALTIME_RELEASE_URL="https://github.com/leancloud/csharp-sdk/releases/download/$version/LeanCloud-SDK-Realtime-Unity.zip"

REPO_GIT_URL="git@github.com:leancloud/csharp-sdk.git"

UNITY_PATH="/Applications/Unity/Hub/Editor/2020.3.5f1c1/Unity.app/Contents/MacOS/Unity"

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

# 生成 .meta 文件并 push 到 GitHub
deploy() {
  upmPath=$1
  tagPrefix=$1

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

  # push 到 GitHub
  upmTag=$tagPrefix-$version
  cd $upmPath

  git init
  git config user.name "leancloud-bot";
  git config user.email "ci@leancloud.cn";
  git add .
  git commit -m $version .;
  git tag $upmTag
  # git push origin $version
  git push -f $REPO_GIT_URL $upmTag

  cd ..
  rm -rf $upmPath
}

if [[ !($version =~ $VERSION_REGEX) ]]; then
  echo 'invalid version'
  exit
fi

upmStoragePath="upm-storage"
upmRealtimePath="upm-realtime"

mkdir $upmStorage && mkdir $upmRealtime

download $STORAGE_RELEASE_URL $upmStoragePath
download $REALTIME_RELEASE_URL $upmRealtimePath

diff $upmStoragePath/Plugins $upmRealtimePath/Plugins 

package ./Unity/storage.package.json $upmStoragePath
package ./Unity/realtime.package.json $upmRealtimePath

deploy $upmStoragePath
deploy $upmRealtimePath
