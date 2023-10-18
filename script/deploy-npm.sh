#!/bin/sh
# 发布 npm
# sh ./script/deploy_npm.sh registry.npmjs.org {authToken} ./Assets/TapTap/Common 
# sh ./script/deploy_npm.sh nexus.tapsvc.com/repository/npm-registry {authToken} ./Assets/TapTap/Common

# 接收参数

echo $1
echo $2
echo $3

# 仓库
registry=$1

# 认证方式
authToken=$2

# 模块目录
modulePath=$3

echo "Package: $modulePath"

cd $modulePath

echo email=bot@xd.com > .npmrc
echo registry=https://$registry/ >> .npmrc
echo //$registry/:always-auth=true >> .npmrc
echo //$registry/:_authToken=$authToken >> .npmrc

# 发布
npm publish --access public

rm -rf .npmrc

cd -