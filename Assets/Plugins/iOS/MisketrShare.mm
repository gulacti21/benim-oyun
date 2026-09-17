#import <UIKit/UIKit.h>

// PAYLAŞ KARTI: iOS paylaşım menüsünü (Instagram, WhatsApp, Fotoğraflar...) açar.
static UIViewController* MisketrTopController()
{
    UIWindow* window = nil;
    for (UIScene* scene in [UIApplication sharedApplication].connectedScenes) {
        if (![scene isKindOfClass:[UIWindowScene class]]) continue;
        for (UIWindow* w in ((UIWindowScene*)scene).windows) {
            if (w.isKeyWindow) { window = w; break; }
        }
        if (window) break;
    }
    UIViewController* vc = window.rootViewController;
    while (vc.presentedViewController) vc = vc.presentedViewController;
    return vc;
}

extern "C" void MisketrShareImage(const char* path, const char* text)
{
    NSString* file = [NSString stringWithUTF8String:path];
    UIImage* image = [UIImage imageWithContentsOfFile:file];
    if (image == nil) return;
    NSMutableArray* items = [NSMutableArray arrayWithObject:image];
    if (text != NULL && strlen(text) > 0) [items addObject:[NSString stringWithUTF8String:text]];

    UIViewController* root = MisketrTopController();
    if (root == nil) return;
    UIActivityViewController* share = [[UIActivityViewController alloc] initWithActivityItems:items applicationActivities:nil];
    if (share.popoverPresentationController != nil) {
        share.popoverPresentationController.sourceView = root.view;
        share.popoverPresentationController.sourceRect = CGRectMake(root.view.bounds.size.width / 2, root.view.bounds.size.height / 2, 1, 1);
    }
    [root presentViewController:share animated:YES completion:nil];
}
