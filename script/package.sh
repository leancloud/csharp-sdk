#!/bin/sh

pack() {
    local path=$1;
    local dir=$2;
    local output=$3;
    mkdir $dir
    rsync -avz $path $dir
    zip -r $output $dir
    rm -r $dir
}

# Storage
pack ./Storage/Storage/bin/Release/netstandard2.0/ ./DLLs LeanCloud-SDK-Storage-Standard.zip
pack ./Storage/Storage-Unity/bin/Release/netstandard2.0/ ./Plugins LeanCloud-SDK-Storage-Unity.zip

# Realtime
pack ./Realtime/Realtime/bin/Release/netstandard2.0/ ./DLLs LeanCloud-SDK-Realtime-Standard.zip
pack ./Realtime/Realtime-Unity/bin/Release/netstandard2.0/ ./Plugins LeanCloud-SDK-Realtime-Unity.zip