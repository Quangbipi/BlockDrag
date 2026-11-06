# Import package Analytics và event mở game lần đầu — Implementation Plan

**Ngày:** 2026-11-07
**Design:** [2026-11-07-quang-analytics-package-design.md](../designs/2026-11-07-quang-analytics-package-design.md)
**Trạng thái:** Đã triển khai xong. Bước 9 do bạn tự kiểm tra trên thiết bị và xác nhận ổn. iOS chưa được kiểm tra.

## Mục tiêu

Cài `com.quang.services.analytics` 0.5.1, bật backend Firebase, và xác nhận `ftu_open_app` được gửi ở lần mở game đầu tiên. Không có code C# mới.

## File bị ảnh hưởng

| File | Thay đổi |
|---|---|
| `GooglePackages/com.google.firebase.messaging-13.17.0.tgz` | Mới, khoảng 1,2 MB |
| `GooglePackages/com.google.firebase.remote-config-13.17.0.tgz` | Mới, khoảng 1,6 MB |
| `Packages/manifest.json` | Thêm 3 dòng |
| `Packages/packages-lock.json` | Unity tự cập nhật |
| `Assets/Resources/QuangAnalyticsSettings.asset` (và `.meta`) | Mới, bật dòng Firebase |
| `ProjectSettings/ProjectSettings.asset` | Thêm define `QUANG_ANALYTICS_FIREBASE` cho Android, iOS, Standalone |
| `Assets/Plugins/Android/mainTemplate.gradle`, file output của resolver | External Dependency Manager tự cập nhật |
| `.agents/rules/architecture.md` | Ghi nhận package đã cài và cách gửi event |
| `Assets/Plugins/Android/AndroidManifest.xml`, `MessagingUnityPlayerActivity.java` (và `.meta`) | **Phát sinh khi triển khai.** Firebase Messaging tự sinh, đổi activity khởi động sang `MessagingUnityPlayerActivity` |
| `Assets/GeneratedLocalRepo/Firebase/m2repository/.../firebase-{config,messaging}-unity*`, `settingsTemplate.gradle`, `ProjectSettings/AndroidResolverDependencies.xml` | **Phát sinh khi triển khai.** Output của resolver |

Không sửa scene, prefab, hay code trong `Assets/_Game`.

## Các bước

### 1. Kiểm tra trước khi làm

- Unity Editor đang mở project qua MCP, Console không có lỗi biên dịch.
- Ghi lại `git status` để phân biệt với các thay đổi chưa commit của bạn (font, `EditorBuildSettings`, `ProjectSettings`).

### 2. Thêm 2 SDK Firebase

1. Tải 2 file `.tgz` từ `dl.google.com/games/registry/unity/` vào một thư mục trống trong scratchpad.
2. Đọc `package.json` trong từng file, xác nhận đúng tên package, phiên bản 13.17.0, và phụ thuộc `com.google.firebase.app` 13.17.0.
3. Chuyển vào `GooglePackages/`.

### 3. Sửa `Packages/manifest.json`

```json
"com.quang.services.analytics": "https://github.com/Quangbipi/Package_Release.git?path=Packages/com.quang.services.analytics#release-2026.11.03.2",
"com.google.firebase.messaging": "file:../GooglePackages/com.google.firebase.messaging-13.17.0.tgz",
"com.google.firebase.remote-config": "file:../GooglePackages/com.google.firebase.remote-config-13.17.0.tgz",
```

Sau đó refresh Unity, chờ resolve và biên dịch xong, rồi đọc Console.

**Kiểm tra:** không có lỗi, và menu `Quang/Analytics/Settings` xuất hiện.

### 4. Resolve Android dependencies

Chạy `Assets/External Dependency Manager/Android Resolver/Force Resolve`.

**Kiểm tra:** `mainTemplate.gradle` có thêm `firebase-messaging` và `firebase-config`, các dòng hiện có không bị mất.

### 5. Tạo settings và bật define

1. Gọi `AnalyticsSettingsWindow.LoadOrCreate()` để tạo asset, đặt dòng Firebase thành `enabled`, giữ category mặc định `All`.
2. Gọi `AnalyticsSettingsWindow.ApplyDefines(settings)`. Unity sẽ biên dịch lại.
3. Xem diff `ProjectSettings.asset`, xác nhận phần mình thêm chỉ là define.

Bước này phải làm sau bước 3 để không gặp lỗi biên dịch do thiếu SDK.

**Kiểm tra:** assembly `Quang.Analytics.Integration.Firebase` biên dịch được, Console không có lỗi.

### 6. Kiểm tra trong Editor

Chạy Play từ `LoadStart` 2 lần liên tiếp.

| Kiểm tra | Kỳ vọng |
|---|---|
| `Locator.Analytics` | Khác `null` |
| GameObject `[QuangAnalyticsTracking]` | Có trong scene |
| `ctx.cold_starts` trong save `analytics-tracking` | Lần 1 là `1`, lần 2 là `2` |
| Console | Không có lỗi `[Analytics]`, không có warning "Tracking state store unavailable" |

`ctx.cold_starts` bằng `1` xác nhận lần chạy đó đi đúng nhánh `ftu_open_app` với save mới. Editor không chứng minh được event đã lên Firebase, việc đó nằm ở bước 9.

Nếu máy đã có save `analytics-tracking` sẵn, mình sẽ hỏi bạn trước khi xóa.

### 7. Chạy test

Chạy toàn bộ EditMode tests của project, so với kết quả trước khi đổi.

### 8. Cập nhật tài liệu kiến trúc

Trong `.agents/rules/architecture.md`, ghi rõ:

- `com.quang.services.analytics` đã được cài, chỉ bật backend Firebase qua `QUANG_ANALYTICS_FIREBASE`.
- `AnalyticsBootstrap` tự gán `Locator.Analytics`, không cần prefab.
- Package gửi sẵn `ftu_open_app` và `open_app`.

### 9. Kiểm tra trên thiết bị Android

Cần điện thoại Android cắm vào máy. Nếu không có thiết bị hoặc keystore, mình sẽ báo rõ là bước này chưa làm và gửi bạn đúng các lệnh dưới.

1. Build APK ra scratchpad, không ghi đè `Build/BlockDrag.apk`.
2. Gỡ app cũ trên thiết bị rồi cài bản mới. Lệnh `adb uninstall` xóa dữ liệu app trên máy đó, nên mình sẽ hỏi bạn trước khi chạy.
3. Bật log và mở app:

```sh
adb shell setprop debug.firebase.analytics.app com.bipibupu.ComboBlockInfiniteGrid
adb shell setprop log.tag.FA VERBOSE
adb logcat -s FA FA-SVC | grep -E "ftu_open_app|open_app"
```

| Kiểm tra | Kỳ vọng |
|---|---|
| Lần mở đầu | Hộp thoại xin quyền thông báo xuất hiện (Android 13 trở lên), log có `ftu_open_app` |
| Tắt hẳn rồi mở lại | Log có `open_app`, không có `ftu_open_app` |
| Firebase DebugView | Thấy `ftu_open_app` (bạn xác nhận, vì mình không có quyền vào Console) |

## Rủi ro và cách xử lý

| Rủi ro | Cách xử lý |
|---|---|
| Gradle lỗi do Messaging hoặc Remote Config xung đột phiên bản | Đọc lỗi Gradle, chạy lại resolver. Nếu cần đổi phiên bản hay sửa template thủ công thì dừng lại hỏi bạn |
| Push notification chưa dùng được đầy đủ | Không xử lý: game chưa dùng push. Package chỉ cần SDK để biên dịch và chạy |
| Lỗi biên dịch khi bật define trên iOS | Chưa kiểm tra iOS trong task này, mình sẽ ghi rõ khi báo kết quả |

## Hoàn tác

1. Xóa define `QUANG_ANALYTICS_FIREBASE`.
2. Xóa 3 dòng trong manifest và 2 file `.tgz`.
3. Xóa `QuangAnalyticsSettings.asset`.
4. Chạy lại Force Resolve.

## Commit

Mình không commit nếu bạn chưa yêu cầu. Khi commit, `ProjectSettings.asset` cần được tách riêng phần define khỏi thay đổi icon và cursor đang có của bạn.
