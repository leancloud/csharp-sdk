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

+ (instancetype)sharedInstance;

- (void)registerIOSPush;

@end

NS_ASSUME_NONNULL_END
