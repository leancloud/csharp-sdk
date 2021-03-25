#!/bin/sh

pack() {
    local path=$1;
    local dir=$2;
    local output=$3;
    local copyLink=$4;
    mkdir $dir
    rsync -avz $path $dir
    if [ $copyLink == true ] ; then
        cp ./Unity/link.xml $dir
    fi
    zip -r $output $dir
    rm -r $dir
}

# Storage
pack ./Storage/Storage/bin/Release/netstandard2.0/ ./DLLs LeanCloud-SDK-Storage-Standard.zip
pack ./Storage/Storage-Unity/bin/Release/netstandard2.0/ ./Plugins LeanCloud-SDK-Storage-Unity.zip true

# Realtime
pack ./Realtime/Realtime/bin/Release/netstandard2.0/ ./DLLs LeanCloud-SDK-Realtime-Standard.zip
pack ./Realtime/Realtime-Unity/bin/Release/netstandard2.0/ ./Plugins LeanCloud-SDK-Realtime-Unity.zip true

# LiveQuery
pack ./LiveQuery/LiveQuery/bin/Release/netstandard2.0/ ./DLLs LeanCloud-SDK-LiveQuery-Standard.zip
pack ./LiveQuery/LiveQuery-Unity/bin/Release/netstandard2.0/ ./Plugins LeanCloud-SDK-LiveQuery-Unity.zip true

# Engine
pack ./Engine/bin/Release/netcoreapp3.1/ ./DLLs LeanCloud-SDK-Engine-Standard.zip