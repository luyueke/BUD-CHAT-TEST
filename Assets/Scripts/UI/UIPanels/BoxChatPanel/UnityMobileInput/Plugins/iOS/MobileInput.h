// ----------------------------------------------------------------------------
// The MIT License
// UnityMobileInput https://github.com/mopsicus/UnityMobileInput
// Copyright (c) 2018-2020 Mopsicus <mail@mopsicus.ru>
// ----------------------------------------------------------------------------

#import <UIKit/UIKit.h>
#import <Foundation/Foundation.h>
#import "Common.h"

@class UIViewController;

@interface PlaceholderTextView : UITextView
@property(nonatomic, strong) NSString *placeholder;
@property(nonatomic, strong) UIColor *realTextColor UI_APPEARANCE_SELECTOR;
@property(nonatomic, strong) UIColor *placeholderColor UI_APPEARANCE_SELECTOR;
@end

@interface MobileInput : NSObject <UITextFieldDelegate, UITextViewDelegate>

// 类方法
+ (void)init:(UIViewController *)viewController;

+ (void)processMessage:(int)inputId data:(NSString *)data;

+ (void)destroy;

+ (void)setPlugin:(NSString *)name;

// 实例方法
- (id)initWith:(UIViewController *)controller andTag:(int)inputId;

- (void)create:(NSDictionary *)data;

- (void)processData:(NSDictionary *)data;

- (void)showKeyboard:(BOOL)value;

- (BOOL)isFocused;

@end

#if defined(__cplusplus)
extern "C" {
#endif
__attribute__ ((visibility("default")))
void mInputExecute(int inputId, const char *data);
__attribute__ ((visibility("default")))
void mInputDestroy(void);
__attribute__ ((visibility("default")))
void mInputInit(void);

#if defined(__cplusplus)
}
#endif

