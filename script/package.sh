#!/bin/bash

prefix=LeanCloud-SDK

storage=Storage
realtime=Realtime
livequery=LiveQuery

standard=Standard
aot=AOT
unity=Unity

releasePath=bin/Release/netstandard2.0

standardReleasePath=./$storage/$storage.$standard/$releasePath
unityReleasePath=./$storage/$storage.$unity/$releasePath

echo $standardReleasePath/$storage.$standard.dll

pack() {
    local path=$1;
    local dir=$2;
    local output=./release/$3;
    local platform=$4;
    mkdir $dir
    rsync -avz $path $dir
    if [[ $platform == standard ]] ; then
        cp $standardReleasePath/$storage.$standard.dll $dir
        cp $standardReleasePath/$storage.$standard.pdb $dir
    elif [[ $platform == unity ]] ; then
        cp $unityReleasePath/$storage.$unity.dll $dir
        cp $unityReleasePath/$storage.$unity.pdb $dir
        cp ./Unity/link.xml $dir
    fi
    zip -r $output $dir
    rm -r $dir
}

mkdir release

# Storage
pack ./$storage/$storage/$releasePath/ ./DLLs $prefix-$storage-$standard.zip standard
pack ./$storage/$storage.$aot/$releasePath/ ./Plugins $prefix-$storage-$unity.zip unity

# Realtime
pack ./$livequery/$livequery/$releasePath/ ./DLLs $prefix-$realtime-$standard.zip standard
pack ./$livequery/$livequery.$aot/$releasePath/ ./Plugins $prefix-$realtime-$unity.zip unity

# Engine
pack ./Engine/bin/Release/netcoreapp3.1/ ./DLLs LeanCloud-SDK-Engine-Standard.zip