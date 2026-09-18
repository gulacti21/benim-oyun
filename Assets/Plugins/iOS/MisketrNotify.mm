#import <UIKit/UIKit.h>
#import <UserNotifications/UserNotifications.h>

// GÜNLÜK HATIRLATMA: "bugünün bölümü hazır" bildirimi. Sunucu yok, tamamen
// telefonda kurulu yerel bildirim. Kullanıcı izin vermezse hiçbir şey olmaz.
static NSString* const kMisketrDailyId = @"misketr.daily";

extern "C" void MisketrRequestNotifyPermission()
{
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
    [center requestAuthorizationWithOptions:(UNAuthorizationOptionAlert | UNAuthorizationOptionSound | UNAuthorizationOptionBadge)
                          completionHandler:^(BOOL granted, NSError* error) { (void)granted; (void)error; }];
}

extern "C" void MisketrScheduleDaily(int hour, int minute, const char* title, const char* body)
{
    UNUserNotificationCenter* center = [UNUserNotificationCenter currentNotificationCenter];
    [center removePendingNotificationRequestsWithIdentifiers:@[kMisketrDailyId]];

    UNMutableNotificationContent* content = [[UNMutableNotificationContent alloc] init];
    content.title = [NSString stringWithUTF8String:title ? title : ""];
    content.body = [NSString stringWithUTF8String:body ? body : ""];
    content.sound = [UNNotificationSound defaultSound];

    NSDateComponents* when = [[NSDateComponents alloc] init];
    when.hour = hour; when.minute = minute;
    UNCalendarNotificationTrigger* trigger = [UNCalendarNotificationTrigger triggerWithDateMatchingComponents:when repeats:YES];

    UNNotificationRequest* request = [UNNotificationRequest requestWithIdentifier:kMisketrDailyId content:content trigger:trigger];
    [center addNotificationRequestWithCompletionHandler:request withCompletionHandler:^(NSError* error) { (void)error; }];
}

extern "C" void MisketrCancelDaily()
{
    [[UNUserNotificationCenter currentNotificationCenter] removePendingNotificationRequestsWithIdentifiers:@[kMisketrDailyId]];
}
