//
//  PushManager.h
//  PushDemo
//
//  Created by oneRain on 2021/6/7.
//

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@interface PushManager : NSObject

@property (nonatomic, strong) NSString* teamId;

@property (nonatomic, strong) NSDictionary* launchPushData;

+ (instancetype)sharedInstance;

- (void)registerIOSPush;

- (void)getLaunchData:(NSString*)callbackId;

@end

NS_ASSUME_NONNULL_END
