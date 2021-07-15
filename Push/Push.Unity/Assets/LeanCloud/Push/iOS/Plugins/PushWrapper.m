//
//  IOSWrapper.m
//  UnityFramework
//
//  Created by oneRain on 2021/6/8.
//

#import <Foundation/Foundation.h>
#import "PushManager.h"

void _RegisterIOSPush(const char* teamId) {
    [PushManager sharedInstance].teamId = [NSString stringWithUTF8String:teamId];
    [[PushManager sharedInstance] registerIOSPush];
}

void _GetLaunchData(const char* callbackId) {
    [[PushManager sharedInstance] getLaunchData: [NSString stringWithUTF8String:callbackId]];
}
