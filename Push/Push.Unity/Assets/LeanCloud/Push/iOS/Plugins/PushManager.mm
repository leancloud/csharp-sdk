//
//  PushManager.m
//  PushDemo
//
//  Created by oneRain on 2021/6/7.
//

#import "PushManager.h"
#import <UIKit/UIKit.h>
#import <UserNotifications/UserNotifications.h>

#include "Classes/PluginBase/AppDelegateListener.h"
#include "Classes/PluginBase/LifeCycleListener.h"

@implementation PushManager

const char* PUSH_BRIDGE = "__LC_PUSH_BRIDGE__";
const char* ON_REGISTER_PUSH = "OnRegisterPush";
const char* ON_GET_LAUNCH_DATA = "OnGetLaunchData";
const char* ON_RECEIVE_MESSAGE = "OnReceiveMessage";

+ (void)load {
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        NSNotificationCenter* nc = [NSNotificationCenter defaultCenter];
        [nc addObserverForName:UIApplicationDidFinishLaunchingNotification
                        object:nil
                         queue:[NSOperationQueue mainQueue]
                    usingBlock:^(NSNotification * _Nonnull note) {
            NSDictionary* pushData = [note.userInfo objectForKey:UIApplicationLaunchOptionsRemoteNotificationKey];
            if (pushData) {
                [PushManager sharedInstance].launchPushData = pushData;
            }
        }];
        
        // 获得 devicetoken 事件
        [nc addObserverForName:kUnityDidRegisterForRemoteNotificationsWithDeviceToken object:nil queue:[NSOperationQueue mainQueue] usingBlock:^(NSNotification * _Nonnull note) {
            NSLog(@"didRegisterForRemoteNotificationsWithDeviceToken");
            if ([note.userInfo isKindOfClass: [NSData class]] &&
                [PushManager sharedInstance].teamId) {
                NSString* deviceToken = [PushManager hexadecimalStringFromData:(NSData*)note.userInfo];
                NSDictionary* deviceInfo = @{
                    @"deviceType": [PushManager deviceType],
                    @"deviceToken": deviceToken,
                    @"apnsTeamId": [PushManager sharedInstance].teamId,
                    @"apnsTopic": [[NSBundle mainBundle] bundleIdentifier],
                    @"timeZone": [[NSTimeZone systemTimeZone] name]
                };
                NSError* error;
                NSData* jsonData = [NSJSONSerialization dataWithJSONObject:deviceInfo options:NSJSONWritingPrettyPrinted error:&error];
                if (!error) {
                    NSString* json = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
                    NSLog(json);
                    UnitySendMessage(PUSH_BRIDGE, ON_REGISTER_PUSH, [json UTF8String]);
                }
            }
        }];
        // 收到远程通知事件
        [nc addObserverForName:kUnityDidReceiveRemoteNotification object:nil queue:[NSOperationQueue mainQueue] usingBlock:^(NSNotification * _Nonnull note) {
            // 解析
            NSDictionary* pushData = note.userInfo;
            [PushManager sharedInstance].launchPushData = pushData;
            
            NSError* error;
            NSData* jsonData = [NSJSONSerialization dataWithJSONObject:pushData options:NSJSONWritingPrettyPrinted error:&error];
            if (!error) {
                NSString* json = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
                UnitySendMessage(PUSH_BRIDGE, ON_RECEIVE_MESSAGE, [json UTF8String]);
            }
        }];
    });
}

+ (instancetype)sharedInstance {
    static PushManager* sharedInstance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        sharedInstance = [[PushManager alloc] init];
    });
    return sharedInstance;
}

- (void)registerIOSPush {
    [[UNUserNotificationCenter currentNotificationCenter] getNotificationSettingsWithCompletionHandler:^(UNNotificationSettings * _Nonnull settings) {
        switch ([settings authorizationStatus]) {
            case UNAuthorizationStatusAuthorized:
                dispatch_async(dispatch_get_main_queue(), ^{
                    [[UIApplication sharedApplication] registerForRemoteNotifications];
                });
                break;
            case UNAuthorizationStatusNotDetermined:
                [[UNUserNotificationCenter currentNotificationCenter] requestAuthorizationWithOptions:UNAuthorizationOptionBadge | UNAuthorizationOptionSound | UNAuthorizationOptionAlert completionHandler:^(BOOL granted, NSError * _Nullable error) {
                    if (granted) {
                        dispatch_async(dispatch_get_main_queue(), ^{
                            [[UIApplication sharedApplication] registerForRemoteNotifications];
                        });
                    }
                }];
                break;
            default:
                break;
        }
    }];
}

- (void)getLaunchData:(NSString*)callbackId {
    NSMutableDictionary* data = [NSMutableDictionary dictionaryWithDictionary:@{@"callbackId": callbackId}];
    if (_launchPushData) {
        [data addEntriesFromDictionary:_launchPushData];
        _launchPushData = nil;
    }
    NSError* error;
    NSData* jsonData = [NSJSONSerialization dataWithJSONObject:data options:NSJSONWritingPrettyPrinted error:&error];
    if (!error) {
        NSString* json = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
        UnitySendMessage(PUSH_BRIDGE, ON_GET_LAUNCH_DATA, [json UTF8String]);
    }
}

- (void)setIconBadgeNumber:(NSInteger)number {
    [[UIApplication sharedApplication] setApplicationIconBadgeNumber:number];
}

+ (NSString *)hexadecimalStringFromData:(NSData *)data {
    NSUInteger dataLength = data.length;
    if (!dataLength) {
        return nil;
    }
    const unsigned char* dataBuffer = (unsigned char*)data.bytes;
    NSMutableString *hexString = [NSMutableString stringWithCapacity:(dataLength * 2)];
    for (int i = 0; i < dataLength; ++i) {
        [hexString appendFormat:@"%02.2hhx", dataBuffer[i]];
    }
    return hexString;
}

+ (NSString *)deviceType
{
#if TARGET_OS_TV
    return @"tvos";
#elif TARGET_OS_WATCH
    return @"watchos";
#elif TARGET_OS_IOS
    return @"ios";
#elif TARGET_OS_OSX
    return @"macos";
#else
    return nil;
#endif
}

@end
