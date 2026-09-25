#import <Foundation/Foundation.h>
#import <GameKit/GameKit.h>
#import <UIKit/UIKit.h>

// iCLOUD KAYDI + GAME CENTER. C# tarafı: Assets/Scripts/Mahalle/MisketrCloud.cs
// iCloud: NSUbiquitousKeyValueStore (kayıt JSON'u birkaç KB, sınır 1 MB). Yetki
// (entitlement) yoksa değerler boş döner, hiçbir şey çökmez.
// Game Center: yetki yoksa giriş sessizce başarısız olur.

static bool gCloudChanged = false;
static bool gCloudObserving = false;

static UIViewController* MisketrCloudTopController()
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

static char* MisketrCloudCopy(NSString* value)
{
    if (value == nil) return NULL;
    const char* utf8 = value.UTF8String;
    char* copy = (char*)malloc(strlen(utf8) + 1);   // Unity serbest bırakır
    strcpy(copy, utf8);
    return copy;
}

extern "C" void MisketrCloudStart()
{
    NSUbiquitousKeyValueStore* store = [NSUbiquitousKeyValueStore defaultStore];
    if (!gCloudObserving) {
        gCloudObserving = true;
        [[NSNotificationCenter defaultCenter] addObserverForName:NSUbiquitousKeyValueStoreDidChangeExternallyNotification
                                                          object:store queue:nil
                                                      usingBlock:^(NSNotification* note) { gCloudChanged = true; }];
    }
    [store synchronize];
}

extern "C" char* MisketrCloudGet(const char* key)
{
    if (key == NULL) return NULL;
    return MisketrCloudCopy([[NSUbiquitousKeyValueStore defaultStore] stringForKey:[NSString stringWithUTF8String:key]]);
}

extern "C" void MisketrCloudSet(const char* key, const char* value)
{
    if (key == NULL || value == NULL) return;
    [[NSUbiquitousKeyValueStore defaultStore] setString:[NSString stringWithUTF8String:value]
                                                 forKey:[NSString stringWithUTF8String:key]];
}

// Başka cihazdan yeni kayıt geldi mi (bir kez okunur, sonra sıfırlanır).
extern "C" bool MisketrCloudConsumeChanged()
{
    bool changed = gCloudChanged;
    gCloudChanged = false;
    return changed;
}

// ---------------------------------------------------------------- Game Center

@interface MisketrGameCenterDelegate : NSObject <GKGameCenterControllerDelegate>
@end
@implementation MisketrGameCenterDelegate
- (void)gameCenterViewControllerDidFinish:(GKGameCenterViewController*)controller
{
    [controller dismissViewControllerAnimated:YES completion:nil];
}
@end

static MisketrGameCenterDelegate* gGameCenterDelegate = nil;

extern "C" void MisketrGcAuthenticate()
{
    GKLocalPlayer* player = [GKLocalPlayer localPlayer];
    player.authenticateHandler = ^(UIViewController* login, NSError* error) {
        if (login != nil) {
            UIViewController* top = MisketrCloudTopController();
            if (top != nil) [top presentViewController:login animated:YES completion:nil];
        }
    };
}

extern "C" bool MisketrGcReady()
{
    return [GKLocalPlayer localPlayer].isAuthenticated;
}

extern "C" void MisketrGcScore(const char* board, int value)
{
    if (board == NULL || ![GKLocalPlayer localPlayer].isAuthenticated) return;
    [GKLeaderboard submitScore:value context:0 player:[GKLocalPlayer localPlayer]
                leaderboardIDs:@[[NSString stringWithUTF8String:board]]
             completionHandler:^(NSError* error) {}];
}

extern "C" void MisketrGcAchievement(const char* identifier, double percent)
{
    if (identifier == NULL || ![GKLocalPlayer localPlayer].isAuthenticated) return;
    GKAchievement* achievement = [[GKAchievement alloc] initWithIdentifier:[NSString stringWithUTF8String:identifier]];
    achievement.percentComplete = percent;
    achievement.showsCompletionBanner = YES;
    [GKAchievement reportAchievements:@[achievement] withCompletionHandler:^(NSError* error) {}];
}

extern "C" void MisketrGcShow()
{
    if (![GKLocalPlayer localPlayer].isAuthenticated) { MisketrGcAuthenticate(); return; }
    UIViewController* top = MisketrCloudTopController();
    if (top == nil) return;
    if (gGameCenterDelegate == nil) gGameCenterDelegate = [[MisketrGameCenterDelegate alloc] init];
    GKGameCenterViewController* view = [[GKGameCenterViewController alloc] initWithState:GKGameCenterViewControllerStateDefault];
    view.gameCenterDelegate = gGameCenterDelegate;
    [top presentViewController:view animated:YES completion:nil];
}
