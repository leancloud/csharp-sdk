//
//  PushManager.h
//  PushDemo
//
//  Created by oneRain on 2021/6/7.
//

#import <Foundation/Foundation.h>
#import <UserNotifications/UserNotifications.h>

NS_ASSUME_NONNULL_BEGIN

@interface PushManager : NSObject<UNUserNotificationCenterDelegate>

@property(nonatomic, strong) NSString *teamId;

@property(nonatomic, strong) NSDictionary *launchPushData;

@property(nonatomic) UNNotificationPresentationOptions option;

+ (instancetype)sharedInstance;

- (void)registerIOSPush;

- (void)getLaunchData:(NSString *)callbackId;

- (void)setIconBadgeNumber:(NSInteger)number;

- (void)setNotificationPresentationOption:(NSInteger)option;

@end

NS_ASSUME_NONNULL_END
