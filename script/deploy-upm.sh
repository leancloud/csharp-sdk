#!/bin/sh
# 发布 upm

REPO_GIT_URL="git@github.com:leancloud/csharp-sdk-upm.git"

upmPath=$1
tagPrefix=$2
version=$3

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

cd -