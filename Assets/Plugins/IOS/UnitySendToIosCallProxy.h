//
//  UnitySendToIosCallProxy.h
//  Buddy_2dx-mobile
//
//  Created by Rock on 2021/6/2.
//

#import <Foundation/Foundation.h>

@protocol UnitySendToIosCallsProtocol

/// 接收来自unity的方法
/// @param funcName 方法名
/// @param argc 此方法参数
- (void)recivedMessageFromUnity:(NSString *)funcName argc:(NSString *)argc;

@end

__attribute__ ((visibility("default")))
@interface UnitySendToIosCallProxy : NSObject

+ (void) registerAPIforNativeCalls:(id<UnitySendToIosCallsProtocol>) aApi;

@end

