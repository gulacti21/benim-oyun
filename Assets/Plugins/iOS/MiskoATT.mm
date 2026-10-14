// 1.1 ATT: reklam kişiselleştirme izni (App Tracking Transparency). MiskoAds.cs çağırır.
#import <AppTrackingTransparency/AppTrackingTransparency.h>

typedef void (*ATTCallback)(int status);

extern "C"
{
    // 0 = sorulmadı, 1 = kısıtlı, 2 = reddedildi, 3 = izin verildi
    int _ATT_GetStatus()
    {
        if (@available(iOS 14, *))
            return (int)[ATTrackingManager trackingAuthorizationStatus];
        return 3;
    }

    void _ATT_RequestPermission(ATTCallback callback)
    {
        if (@available(iOS 14, *))
        {
            [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status)
            {
                dispatch_async(dispatch_get_main_queue(), ^{
                    if (callback) callback((int)status);
                });
            }];
        }
        else if (callback) callback(3);
    }
}
