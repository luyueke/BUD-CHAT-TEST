//
//  UnityToIosCallProxy.m
//  Buddy_2dx-mobile
//
//  Created by Rock on 2021/6/2.
//

#import <Foundation/Foundation.h>
#import "UnitySendToIosCallProxy.h"

@implementation UnitySendToIosCallProxy

id<UnitySendToIosCallsProtocol> api = NULL;
+ (void) registerAPIforNativeCalls:(id<UnitySendToIosCallsProtocol>) aApi
{
	api = aApi;
}

@end


/// C语言实现调用

extern "C" {

void sendMessageToClient(const char * funcName, const char * argc) {
	if (api == NULL) {
		return;
	}

	NSString *func = @"";
	if (funcName != NULL) {
		func = [NSString stringWithUTF8String:funcName];
	}

	NSString *parameter = @"";
	if (argc != NULL) {
		parameter = [NSString stringWithUTF8String:argc];
	}
	return [api recivedMessageFromUnity:func argc:parameter];
}
}
