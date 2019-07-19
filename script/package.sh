#!/bin/sh
mkdir Plugins
rsync -av --exclude='UnityEngine.dll' ./Storage/Storage.PCL/bin/Release/ ./Plugins/
zip -r LeanCloud-CSharp-SDK.zip Plugins
zip -r LeanCloud-Unity-SDK.zip Plugins
rm -r Plugins