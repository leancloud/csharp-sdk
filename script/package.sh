#!/bin/sh
mkdir DLLs
rsync -avz ./Storage/Storage.PCL/bin/Release/ DLLs
rsync -avz ./RTM/RTM.PCL/bin/Release/ DLLs
rsync -avz ./LiveQuery/LiveQuery.PCL/bin/Release/ DLLs
zip -r LeanCloud-Portable-SDK.zip DLLs
rm -r DLLs

mkdir Plugins
rsync -av --exclude='UnityEngine.dll' ./Storage/Storage.Unity/bin/Release/ Plugins
rsync -av --exclude='UnityEngine.dll' ./RTM/RTM.Unity/bin/Release/ Plugins
rsync -av --exclude='UnityEngine.dll' ./LiveQuery/LiveQuery.Unity/bin/Release/ Plugins
zip -r LeanCloud-Unity-SDK.zip Plugins
rm -r Plugins