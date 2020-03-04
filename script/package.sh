#!/bin/sh
mkdir DLLs
rsync -avz ./Storage/bin/Release/netstandard2.0/ DLLs
#rsync -avz ./RTM/RTM.PCL/bin/Release/ DLLs
#rsync -avz ./LiveQuery/LiveQuery.PCL/bin/Release/ DLLs
zip -r LeanCloud-SDK-Standard.zip DLLs
rm -r DLLs

mkdir Plugins
rsync -av --exclude='UnityEngine.dll' ./Storage/bin/Release/netstandard2.0/ Plugins
#rsync -av --exclude='UnityEngine.dll' ./RTM/RTM.Unity/bin/Release/ Plugins
#rsync -av --exclude='UnityEngine.dll' ./LiveQuery/LiveQuery.Unity/bin/Release/ Plugins
zip -r LeanCloud-SDK-Unity.zip Plugins
rm -r Plugins